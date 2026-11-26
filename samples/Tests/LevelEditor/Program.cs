using GameplaySamples.LevelEditor;

void Check(bool passed, string description)
{
    if (!passed)
        throw new InvalidOperationException(description);
}

var board = new LevelBoard();
Check(board.TrySet(7, 4, TileKind.Goal) &&
    board.TrySet(3, 2, TileKind.Spring) &&
    board.TrySet(0, 0, TileKind.Rock), "Paint three tile types.");
Check(!board.TrySet(LevelBoard.Width, 0, TileKind.Goal), "Reject coordinates at the right edge.");
Check(!board.TrySet(0, 0, (TileKind)99) && board.At(0, 0) == TileKind.Rock, "Reject unknown types without changing the board.");

List<LevelTile> preview = board.Snapshot();
Check(preview[0].Kind == TileKind.Rock && preview.SequenceEqual(board.Snapshot()), "Snapshots are stable and row ordered.");
Check(LevelBoard.TryLoad(preview, out LevelBoard reopened, out _) &&
    reopened.Snapshot().SequenceEqual(preview), "Saved tiles reopen to the same grid.");
Check(!LevelBoard.TryLoad(preview.Concat(preview), out _, out string overlap) &&
    overlap.Contains("More than one"), "Overlapping positions fail validation.");
Check(!LevelBoard.TryLoad(new[] { new LevelTile(-1, 0, TileKind.Rock) }, out _, out string bounds) &&
    bounds.Contains("outside"), "Out-of-bounds positions fail validation.");
Check(board.TrySet(0, 0, TileKind.Empty) && board.Snapshot().Count == 2, "Erase a tile.");

var gentleRamp = LevelBoard.CreateDraft(LevelTemplate.GentleRamp);
var brokenBridge = LevelBoard.CreateDraft(LevelTemplate.BrokenBridge);
Check(!gentleRamp.Snapshot().SequenceEqual(brokenBridge.Snapshot()), "The two templates produce different layouts.");
foreach (LevelTemplate template in Enum.GetValues<LevelTemplate>())
{
    var firstDraft = LevelBoard.CreateDraft(template);
    var secondDraft = LevelBoard.CreateDraft(template);
    var original = secondDraft.Snapshot();
    Check(original.Count(tile => tile.Kind == TileKind.Goal) == 1 &&
        original.Any(tile => tile.Kind == TileKind.Rock) &&
        original.Any(tile => tile.Kind == TileKind.Spring), "Each template includes obstacles, a spring and one goal.");

    LevelTile goal = original.Single(tile => tile.Kind == TileKind.Goal);
    Check(firstDraft.TrySet(goal.X, goal.Y, TileKind.Empty), "Paint or erase cells in the copied draft.");
    Check(secondDraft.Snapshot().SequenceEqual(original) &&
        LevelBoard.CreateDraft(template).Snapshot().SequenceEqual(original), "An edit changes neither another draft nor the template.");
}

Console.WriteLine("Level Editor PASS: independent template copies, painting, erasing, stable order, round trip, overlap and bounds validation.");
