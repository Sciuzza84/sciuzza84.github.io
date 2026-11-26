using GameplaySamples.Dungeon;

internal static class Program
{
    private static int Main()
    {
        int[] seeds = Enumerable.Range(-160, 321).Append(int.MinValue).Append(int.MaxValue).ToArray();
        int checkedLayouts = 0;
        int seed = 0;
        int request = 0;

        try
        {
            CheckLengthPresets();
            foreach (int nextSeed in seeds)
            {
                seed = nextSeed;
                DungeonLayout previous = null;
                foreach (int nextRequest in Enumerable.Range(0, 9))
                {
                    request = nextRequest;
                    DungeonLayout layout = DungeonGenerator.Generate(seed, request);
                    ValidateGraph(layout);
                    Require(layout.RequestedBranches == request, "The reported request changed.");
                    Require(layout.PlacedBranches <= request, "More branches were placed than requested.");
                    RequireSameLayout(layout, DungeonGenerator.Generate(seed, request));
                    if (seed == 19 && request == 4)
                    {
                        Require(layout.Rooms.Count == 36 && layout.Edges.Count == 35 &&
                                layout.PlacedBranches == 4 && layout.RejectedCandidates == 3,
                            "The README example has changed. Revisit the map and its published description.");
                    }
                    if (previous != null)
                    {
                        Require(RoomValues(previous).SequenceEqual(RoomValues(layout).Take(previous.Rooms.Count)),
                            "Raising the requested branch count changed the existing rooms.");
                        if (previous.FirstRejection.HasValue)
                            Require(RejectionValue(previous) == RejectionValue(layout),
                                "Raising the requested branch count changed the first rejection.");
                    }
                    previous = layout;
                    checkedLayouts++;
                }
            }

            request = 4;
            var variants = Enumerable.Range(0, 12).Select(s => DungeonGenerator.Generate(s, request)).ToArray();
            Require(variants.Any(layout => layout.PlacedBranches > 0), "All nonzero requests produced a bare route.");
            Require(variants.Skip(1).Any(layout => !RoomValues(layout).SequenceEqual(RoomValues(variants[0]))),
                "Changing the seed never changed the geometry.");

            foreach (int nextRequest in new[] { int.MinValue, int.MaxValue })
            {
                request = nextRequest;
                seed = -23;
                DungeonLayout layout = DungeonGenerator.Generate(seed, request);
                ValidateGraph(layout);
                Require(layout.RequestedBranches == request, "An extreme request lost its original value.");
                Require(layout.PlacedBranches <= 8, "An extreme request exceeded the supported branch budget.");
                if (request < 0)
                    Require(layout.PlacedBranches == 0, "A negative request produced branches.");
                checkedLayouts++;
            }
        }
        catch (Exception failure)
        {
            Console.Error.WriteLine($"FAIL (seed {seed}, requested {request}): {failure.Message}");
            return 1;
        }

        Console.WriteLine($"PASS: {checkedLayouts} layouts. Repeatability, seed variation, grid bounds, unit edges, "
            + "reachability, branches and extreme requests.");
        Console.WriteLine("PASS: branch preset validation, repair, weighted boundaries and 128 weighted layouts.");
        return 0;
    }

    private static void CheckLengthPresets()
    {
        var (rooms, weights) = DungeonBranchPreset.Example(invalid: true);
        DungeonPresetValidation invalid = DungeonBranchPreset.Validate(rooms, weights);
        Require(invalid.Preset == null && invalid.RoomErrors[1].Contains("row 1") &&
            !invalid.ValidWeightTotal && invalid.WeightFeedback == "Total 80%. Add 20 points.",
            "The invalid example must describe both problems and yield no usable preset.");

        rooms[1] = "3";
        var missingWeight = DungeonBranchPreset.Validate(rooms, weights);
        Require(missingWeight.Preset == null && missingWeight.RoomErrors.All(error => error == null),
            "Fixing the duplicate must clear its message while leaving the total error.");
        weights[2] = "90";
        var repaired = DungeonBranchPreset.Validate(rooms, weights);
        Require(repaired.Preset != null && repaired.ValidWeightTotal && repaired.WeightFeedback == "Total 100%.",
            "The edited example must become usable as soon as both errors are corrected.");
        Require(!RoomValues(DungeonGenerator.Generate(19, 4, repaired.Preset)).SequenceEqual(RoomValues(DungeonGenerator.Generate(19, 4))),
            "The documented repair must produce a visibly different map from the default at seed 19.");
        weights[0] = "30";
        Require(DungeonBranchPreset.Validate(rooms, weights).WeightFeedback == "Total 125%. Remove 25 points.",
            "An overweight total must say how much weight to remove.");

        var malformed = DungeonBranchPreset.Validate(new[] { "1", "3", "4" }, new[] { "35.5", "-1", "101" });
        Require(malformed.Preset == null && malformed.RoomErrors[0] != null &&
            malformed.WeightErrors.All(error => error != null) && !malformed.ValidWeightTotal,
            "Each malformed or out-of-range field needs its own error.");

        var (validRooms, validWeights) = DungeonBranchPreset.Example(invalid: false);
        DungeonBranchPreset valid = DungeonBranchPreset.Validate(validRooms, validWeights).Preset;
        Require(valid != null && valid.LengthForRoll(0) == 2 && valid.LengthForRoll(19) == 2 &&
            valid.LengthForRoll(20) == 3 && valid.LengthForRoll(49) == 3 &&
            valid.LengthForRoll(50) == 4 && valid.LengthForRoll(99) == 4,
            "Weight boundaries must include exactly 20%, 30% and 50% of the possible rolls.");

        DungeonBranchPreset shortPreset = DungeonBranchPreset.Validate(validRooms, new[] { "100", "0", "0" }).Preset;
        DungeonBranchPreset longPreset = DungeonBranchPreset.Validate(validRooms, new[] { "0", "0", "100" }).Preset;
        Require(shortPreset.LengthForRoll(99) == 2 && longPreset.LengthForRoll(0) == 4,
            "Zero-weight entries must never be selected.");
        int differences = 0;
        for (int seed = -32; seed < 32; seed++)
        {
            DungeonLayout shortLayout = DungeonGenerator.Generate(seed, 4, shortPreset);
            DungeonLayout longLayout = DungeonGenerator.Generate(seed, 4, longPreset);
            ValidateGraph(shortLayout);
            ValidateGraph(longLayout);
            RequireSameLayout(longLayout, DungeonGenerator.Generate(seed, 4, longPreset));
            Require(RoomValues(shortLayout).Where(room => room.Item3).SequenceEqual(RoomValues(longLayout).Where(room => room.Item3)),
                "Length weights must leave the main route intact.");
            if (!RoomValues(shortLayout).SequenceEqual(RoomValues(longLayout)))
                differences++;
        }
        Require(differences > 0, "Applying different weights must affect the generated map.");
    }

    private static void ValidateGraph(DungeonLayout layout)
    {
        int count = layout.Rooms.Count;
        Require(count >= 2 && layout.Width > 0 && layout.Height > 0, "The map must hold an entrance and a boss.");
        Require(layout.EntranceIndex >= 0 && layout.EntranceIndex < count &&
                layout.BossIndex >= 0 && layout.BossIndex < count && layout.EntranceIndex != layout.BossIndex,
            "Entrance and boss must identify distinct rooms.");
        Require(layout.Rooms[layout.EntranceIndex].IsMainRoute && layout.Rooms[layout.BossIndex].IsMainRoute,
            "The entrance and the boss must be on the main route.");

        var cells = new Dictionary<(int, int), int>();
        for (int i = 0; i < count; i++)
        {
            DungeonRoom room = layout.Rooms[i];
            Require(room.X >= 0 && room.X < layout.Width && room.Y >= 0 && room.Y < layout.Height,
                "A room escaped the grid.");
            Require(cells.TryAdd((room.X, room.Y), i), "Two rooms occupy the same cell.");
        }

        var neighbours = Enumerable.Range(0, count).Select(_ => new List<int>()).ToArray();
        var connections = new HashSet<(int, int)>();
        int branchStarts = 0;
        foreach (DungeonEdge edge in layout.Edges)
        {
            Require(edge.From >= 0 && edge.From < count && edge.To >= 0 && edge.To < count,
                "An edge has a missing endpoint.");
            DungeonRoom from = layout.Rooms[edge.From];
            DungeonRoom to = layout.Rooms[edge.To];
            // With distinct integer cells, unit orthogonal edges can meet only at a shared room.
            Require(Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y) == 1,
                "An edge skips a cell or cuts across the grid.");
            Require(connections.Add((Math.Min(edge.From, edge.To), Math.Max(edge.From, edge.To))),
                "Two connections overlap.");
            neighbours[edge.From].Add(edge.To);
            neighbours[edge.To].Add(edge.From);
            if (from.IsMainRoute != to.IsMainRoute)
                branchStarts++;
        }

        Require(layout.Edges.Count == count - 1, "A tree must have exactly one fewer edge than rooms.");
        Require(branchStarts == layout.PlacedBranches, "Reported branches disagree with the map.");
        Require(layout.RejectedCandidates >= 0, "The rejected candidate count must be nonnegative.");
        Require(layout.FirstRejection.HasValue == (layout.RejectedCandidates > 0),
            "The explanation must exist when a candidate was rejected.");
        if (layout.FirstRejection is DungeonBranchRejection rejection)
        {
            bool isInside = rejection.X >= 0 && rejection.X < layout.Width &&
                            rejection.Y >= 0 && rejection.Y < layout.Height;
            Require(isInside == (rejection.Reason != DungeonRejectionReason.OutsideGrid),
                "The rejection reason disagrees with the candidate position.");
            if (rejection.Reason == DungeonRejectionReason.Occupied)
                Require(cells.ContainsKey((rejection.X, rejection.Y)), "The occupied cell is missing its room.");
        }
        Require(Reachable(neighbours, layout, mainOnly: false).Count == count, "A room is unreachable.");
        var mainRoute = Reachable(neighbours, layout, mainOnly: true);
        Require(mainRoute.Contains(layout.BossIndex) && mainRoute.Count == layout.Rooms.Count(room => room.IsMainRoute),
            "The marked route must connect the entrance, the boss and all main rooms.");
        foreach (int index in mainRoute)
        {
            bool endpoint = index == layout.EntranceIndex || index == layout.BossIndex;
            Require(neighbours[index].Count(mainRoute.Contains) == (endpoint ? 1 : 2),
                "The main route must be one path from entrance to boss.");
        }
        foreach (var cell in cells)
        {
            // Check one direction on each axis so every possible grid neighbour is visited once.
            var (x, y) = cell.Key;
            foreach (var adjacent in new[] { (x + 1, y), (x, y + 1) })
            {
                if (cells.TryGetValue(adjacent, out int other))
                    Require(neighbours[cell.Value].Contains(other), "Two routes touch without a connecting edge.");
            }
        }
    }

    private static HashSet<int> Reachable(List<int>[] neighbours, DungeonLayout layout, bool mainOnly)
    {
        var visited = new HashSet<int> { layout.EntranceIndex };
        var pending = new Queue<int>();
        pending.Enqueue(layout.EntranceIndex);
        while (pending.TryDequeue(out int current))
        {
            foreach (int next in neighbours[current])
            {
                if ((!mainOnly || layout.Rooms[next].IsMainRoute) && visited.Add(next))
                    pending.Enqueue(next);
            }
        }
        return visited;
    }

    private static IEnumerable<(int, int, bool)> RoomValues(DungeonLayout layout) =>
        layout.Rooms.Select(room => (room.X, room.Y, room.IsMainRoute));

    private static IEnumerable<(int, int)> EdgeValues(DungeonLayout layout) =>
        layout.Edges.Select(edge => (edge.From, edge.To));

    private static (int, int, DungeonRejectionReason)? RejectionValue(DungeonLayout layout) =>
        layout.FirstRejection is DungeonBranchRejection rejection ? (rejection.X, rejection.Y, rejection.Reason) : null;

    private static void RequireSameLayout(DungeonLayout first, DungeonLayout second)
    {
        Require(first.Width == second.Width && first.Height == second.Height &&
                first.EntranceIndex == second.EntranceIndex && first.BossIndex == second.BossIndex &&
                first.PlacedBranches == second.PlacedBranches && first.RejectedCandidates == second.RejectedCandidates &&
                RejectionValue(first) == RejectionValue(second) &&
                RoomValues(first).SequenceEqual(RoomValues(second)) && EdgeValues(first).SequenceEqual(EdgeValues(second)),
            "Repeating a seed and request changed the result.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
