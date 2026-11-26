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

    UpdateResult corrupt = InstalledContent.Update(baseline, second.Manifest, second.FetchWithCorruption, () => false);
    Check(corrupt.Outcome == UpdateOutcome.InvalidPayload && corrupt.FailedFile == "palette.txt" &&
        corrupt.VerifiedDownloads == 0 &&
        ReferenceEquals(corrupt.Installed, baseline) && corrupt.Installed.Read("palette.txt") == oldPalette,
        "invalid payload rolls back");

    bool downloaded = false;
    UpdateResult cancelled = InstalledContent.Update(baseline, second.Manifest, name =>
    {
        downloaded = true;
        return second.Fetch(name);
    }, () => downloaded);
    Check(cancelled.Outcome == UpdateOutcome.Cancelled && cancelled.Downloaded == 1 && cancelled.VerifiedDownloads == 1 &&
        ReferenceEquals(cancelled.Installed, baseline), "late cancellation discards verified staging");

    ContentFixture third = ContentFixture.Create(platform, 3);
    var attempted = new List<string>();
    UpdateResult partiallyStaged = InstalledContent.Update(changed.Installed, third.Manifest, name =>
    {
        attempted.Add(name);
        return third.FetchWithCorruption(name);
    }, () => false);
    Check(attempted.SequenceEqual(new[] { "board.txt", "rules.txt" }) &&
        partiallyStaged.Outcome == UpdateOutcome.InvalidPayload && partiallyStaged.FailedFile == "rules.txt" &&
        partiallyStaged.Downloaded == 2 && partiallyStaged.VerifiedDownloads == 1 &&
        ReferenceEquals(partiallyStaged.Installed, changed.Installed) && partiallyStaged.Installed.Version == 2 &&
        partiallyStaged.Installed.Read("board.txt") == second.Fetch("board.txt") &&
        partiallyStaged.Installed.Read("rules.txt") == second.Fetch("rules.txt"), "partial staging never replaces a file");

    UpdateResult retry = InstalledContent.Update(partiallyStaged.Installed, third.Manifest, third.Fetch, () => false);
    Check(retry.Outcome == UpdateOutcome.Applied && retry.Installed.Version == 3 && retry.Downloaded == 2 &&
        retry.VerifiedDownloads == 2 && retry.Reused == 1 &&
        retry.Installed.Read("board.txt") == third.Fetch("board.txt") &&
        retry.Installed.Read("rules.txt") == third.Fetch("rules.txt") &&
        retry.Installed.Read("palette.txt") == second.Fetch("palette.txt"), "clean retry commits both changed files");
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

Console.WriteLine("PASS: initial install, selective transfer, no-op retry, corrupt payload, cancellation, staged rollback, clean retry, and platform isolation.");

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"Content update check failed: {name}");
}
