using System;
using System.Collections.Generic;

namespace GameplaySamples.Dungeon
{
    public static class DungeonGenerator
    {
        public const int MaxBranches = 8;

        // The layout keeps the original request, even when the grid or the cap limits placement.
        public static DungeonLayout Generate(int seed, int requestedBranches) => Generate(seed, requestedBranches, null);

        internal static DungeonLayout Generate(int seed, int requestedBranches, DungeonBranchPreset preset) =>
            new Builder(seed, preset).Build(requestedBranches);

        private sealed class Builder
        {
            private const int Width = 19;
            private const int Height = 13;
            private readonly List<DungeonRoom> rooms = new List<DungeonRoom>();
            private readonly List<DungeonEdge> edges = new List<DungeonEdge>();
            private readonly bool[,] occupied = new bool[Width, Height];
            private readonly DungeonBranchPreset preset;
            private uint randomState;
            private int rejectedCandidates;
            private DungeonBranchRejection? firstRejection;

            internal Builder(int seed, DungeonBranchPreset preset)
            {
                this.preset = preset;
                randomState = unchecked((uint)seed * 2654435761u);
            }

            internal DungeonLayout Build(int requestedBranches)
            {
                BuildMainRoute();
                int boss = rooms.Count - 1;
                List<BranchSite> sites = FindSites();
                Shuffle(sites);

                int goal = Math.Max(0, Math.Min(requestedBranches, MaxBranches));
                var usedRoots = new HashSet<int>();
                foreach (BranchSite site in sites)
                {
                    if (usedRoots.Count == goal)
                        break;
                    if (usedRoots.Contains(site.Root))
                        continue;

                    int length = preset == null ? 2 + Next(3) : preset.LengthForRoll(Next(100));
                    int tailLength = Next(3);
                    int turn = Next(2) == 0 ? -1 : 1;
                    bool placed = TryBranch(site, length, tailLength, turn);
                    // A narrow gap can still fit a short straight branch. Each site gets at most two tries.
                    if (!placed && (length != 2 || tailLength != 0))
                        placed = TryBranch(site, 2, 0, turn);
                    if (placed)
                        usedRoots.Add(site.Root);
                }

                return new DungeonLayout(Width, Height, rooms, edges, boss,
                    requestedBranches, usedRoots.Count, rejectedCandidates, firstRejection);
            }

            private void BuildMainRoute()
            {
                int y = Height / 2;
                Append(new DungeonRoom(1, y, true), -1);
                for (int x = 2; x < Width - 1; x++)
                {
                    Append(new DungeonRoom(x, y, true), rooms.Count - 1);
                    // Leave space between turns and reserve the outer rows for side paths.
                    if (x % 4 != 0 || x >= Width - 3)
                        continue;
                    int direction = Next(2) == 0 ? -1 : 1;
                    int targetY = Math.Max(4, Math.Min(Height - 5, y + direction * (1 + Next(2))));
                    while (y != targetY)
                    {
                        y += direction;
                        Append(new DungeonRoom(x, y, true), rooms.Count - 1);
                    }
                }
            }

            private List<BranchSite> FindSites()
            {
                var sites = new List<BranchSite>();
                for (int root = 1; root < rooms.Count - 1; root++)
                {
                    Consider(1, 0);
                    Consider(-1, 0);
                    Consider(0, 1);
                    Consider(0, -1);

                    void Consider(int dx, int dy)
                    {
                        DungeonRoom room = rooms[root];
                        if (GetRejection(room.X + dx, room.Y + dy, 1) == null)
                            sites.Add(new BranchSite(root, dx, dy));
                    }
                }
                return sites;
            }

            private bool TryBranch(BranchSite site, int length, int tailLength, int turn)
            {
                int x = rooms[site.Root].X;
                int y = rooms[site.Root].Y;
                var path = new List<DungeonRoom>(length + tailLength);
                for (int step = 0; step < length + tailLength; step++)
                {
                    bool onTail = step >= length;
                    x += onTail ? -site.Dy * turn : site.Dx;
                    y += onTail ? site.Dx * turn : site.Dy;
                    // A single right-angle bend cannot touch its own earlier cells. Only its first
                    // cell may touch the existing graph, so no accidental junction or loop can form.
                    DungeonRejectionReason? rejection = GetRejection(x, y, step == 0 ? 1 : 0);
                    if (rejection.HasValue)
                    {
                        rejectedCandidates++;
                        firstRejection ??= new DungeonBranchRejection(x, y, rejection.Value);
                        return false;
                    }
                    path.Add(new DungeonRoom(x, y, false));
                }

                // Commit only a complete branch. An invalid tail leaves the previous map intact.
                int parent = site.Root;
                foreach (DungeonRoom room in path)
                    parent = Append(room, parent);
                return true;
            }

            private DungeonRejectionReason? GetRejection(int x, int y, int expectedNeighbours)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                    return DungeonRejectionReason.OutsideGrid;
                if (occupied[x, y])
                    return DungeonRejectionReason.Occupied;
                int neighbours = 0;
                if (x > 0 && occupied[x - 1, y]) neighbours++;
                if (x + 1 < Width && occupied[x + 1, y]) neighbours++;
                if (y > 0 && occupied[x, y - 1]) neighbours++;
                if (y + 1 < Height && occupied[x, y + 1]) neighbours++;
                return neighbours == expectedNeighbours ? (DungeonRejectionReason?)null : DungeonRejectionReason.InsufficientClearance;
            }

            private int Append(DungeonRoom room, int parent)
            {
                int index = rooms.Count;
                occupied[room.X, room.Y] = true;
                rooms.Add(room);
                if (parent >= 0)
                    edges.Add(new DungeonEdge(parent, index));
                return index;
            }

            private void Shuffle(List<BranchSite> sites)
            {
                for (int i = sites.Count - 1; i > 0; i--)
                {
                    int j = Next(i + 1);
                    BranchSite other = sites[j];
                    sites[j] = sites[i];
                    sites[i] = other;
                }
            }

            // Use a specified 32-bit sequence so seeds reproduce across Unity, .NET and Web builds.
            private int Next(int exclusiveMax)
            {
                randomState = unchecked(randomState * 1664525u + 1013904223u);
                return (int)(((ulong)randomState * (uint)exclusiveMax) >> 32);
            }
        }

        private readonly struct BranchSite
        {
            public readonly int Root;
            public readonly int Dx;
            public readonly int Dy;

            public BranchSite(int root, int dx, int dy)
            {
                Root = root;
                Dx = dx;
                Dy = dy;
            }
        }
    }
}
