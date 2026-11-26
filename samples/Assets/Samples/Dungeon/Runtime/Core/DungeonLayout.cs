using System.Collections.Generic;

namespace GameplaySamples.Dungeon
{
    public readonly struct DungeonRoom
    {
        public int X { get; }
        public int Y { get; }
        public bool IsMainRoute { get; }

        internal DungeonRoom(int x, int y, bool isMainRoute)
        {
            X = x;
            Y = y;
            IsMainRoute = isMainRoute;
        }
    }

    // Endpoints are indices in DungeonLayout.Rooms.
    public readonly struct DungeonEdge
    {
        public int From { get; }
        public int To { get; }

        internal DungeonEdge(int from, int to)
        {
            From = from;
            To = to;
        }
    }

    public enum DungeonRejectionReason
    {
        OutsideGrid,
        Occupied,
        InsufficientClearance
    }

    public readonly struct DungeonBranchRejection
    {
        public int X { get; }
        public int Y { get; }
        public DungeonRejectionReason Reason { get; }

        internal DungeonBranchRejection(int x, int y, DungeonRejectionReason reason)
        {
            X = x;
            Y = y;
            Reason = reason;
        }
    }

    public sealed class DungeonLayout
    {
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<DungeonRoom> Rooms { get; }
        public IReadOnlyList<DungeonEdge> Edges { get; }
        public int EntranceIndex => 0;
        public int BossIndex { get; }
        public int RequestedBranches { get; }
        public int PlacedBranches { get; }
        public int RejectedCandidates { get; }
        public DungeonBranchRejection? FirstRejection { get; }

        internal DungeonLayout(int width, int height, List<DungeonRoom> rooms, List<DungeonEdge> edges,
            int bossIndex, int requestedBranches, int placedBranches, int rejectedCandidates,
            DungeonBranchRejection? firstRejection)
        {
            Width = width;
            Height = height;
            Rooms = rooms.AsReadOnly();
            Edges = edges.AsReadOnly();
            BossIndex = bossIndex;
            RequestedBranches = requestedBranches;
            PlacedBranches = placedBranches;
            RejectedCandidates = rejectedCandidates;
            FirstRejection = firstRejection;
        }
    }
}
