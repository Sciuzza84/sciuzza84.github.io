using GameplaySamples.ContentUpdates;

foreach (ContentPlatform platform in Enum.GetValues<ContentPlatform>())
{
    ContentFixture first = ContentFixture.Create(platform, 1);
    ContentFixture second = ContentFixture.Create(platform, 2);
    InstalledContent empty = InstalledContent.Empty(platform);

    UpdateResult bootstrap = InstalledContent.Update(empty, first.Manifest, first.Fetch, () => false);
    Check(bootstrap.Outcome == UpdateOutcome.Applied && bootstrap.Downloaded == 3 && bootstrap.Reused == 0 &&
        bootstrap.Installed.Version == 1 && bootstrap.Installed.Count == 3, "initial install");
    InstalledContent baseline = bootstrap.Installed;
    string oldPalette = baseline.Read("palette.txt");

    UpdateResult changed = InstalledContent.Update(baseline, second.Manifest, second.Fetch, () => false);
    Check(changed.Outcome == UpdateOutcome.Applied && changed.Downloaded == 1 && changed.Reused == 2 &&
        changed.Installed.Version == 2 && changed.Installed.Read("palette.txt") == second.Fetch("palette.txt"),
        "selective update");
    Check(baseline.Read("palette.txt") == oldPalette, "prior snapshot remains readable");

    UpdateResult repeated = InstalledContent.Update(changed.Installed, second.Manifest,
        _ => throw new InvalidOperationException("No download should occur"), () => false);
    Check(repeated.Outcome == UpdateOutcome.Applied && repeated.Downloaded == 0 && repeated.Reused == 3,
        "repeating the same version needs no transfer");

    UpdateResult corrupt = InstalledContent.Update(baseline, second.Manifest, _ => "wrong checksum", () => false);
    Check(corrupt.Outcome == UpdateOutcome.InvalidPayload && corrupt.FailedFile == "palette.txt" &&
        ReferenceEquals(corrupt.Installed, baseline) && corrupt.Installed.Read("palette.txt") == oldPalette,
        "invalid payload rolls back");

    bool downloaded = false;
    UpdateResult cancelled = InstalledContent.Update(baseline, second.Manifest, name =>
    {
        downloaded = true;
        return second.Fetch(name);
    }, () => downloaded);
    Check(cancelled.Outcome == UpdateOutcome.Cancelled && cancelled.Downloaded == 1 &&
        ReferenceEquals(cancelled.Installed, baseline), "late cancellation discards verified staging");

    var broken = new ContentManifest(platform, 3, new[]
    {
        new ContentEntry("board.txt", ContentDigest.For("valid new board")),
        new ContentEntry("rules.txt", ContentDigest.For("expected replacement"))
    });
    UpdateResult partiallyStaged = InstalledContent.Update(baseline, broken,
        name => name == "board.txt" ? "valid new board" : "invalid rules", () => false);
    Check(partiallyStaged.Outcome == UpdateOutcome.InvalidPayload && partiallyStaged.Downloaded == 2 &&
        ReferenceEquals(partiallyStaged.Installed, baseline) &&
        partiallyStaged.Installed.Read("board.txt") == first.Fetch("board.txt"), "partial staging never replaces a file");
}

ContentManifest ios = ContentFixture.Create(ContentPlatform.Ios, 1).Manifest;
try
{
    InstalledContent.Update(InstalledContent.Empty(ContentPlatform.Android), ios, _ => "", () => false);
    throw new InvalidOperationException("Android installation accepted an iOS manifest.");
}
catch (ArgumentException)
{
}

Console.WriteLine("PASS: initial install, selective transfer, no-op retry, corrupt payload, cancellation, staged rollback, and platform isolation.");

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"Content update check failed: {name}");
}
