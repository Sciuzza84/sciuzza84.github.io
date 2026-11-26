using System;
using System.Collections.Generic;

namespace GameplaySamples.LevelEditor
{
    public enum TileKind
    {
        Empty,
        Rock,
        Spring,
        Goal
    }

    public enum LevelTemplate
    {
        GentleRamp,
        BrokenBridge
    }

    [Serializable]
    public struct LevelTile
    {
        public int X;
        public int Y;
        public TileKind Kind;

        public LevelTile(int x, int y, TileKind kind)
        {
            X = x;
            Y = y;
            Kind = kind;
        }
    }

    public sealed class LevelBoard
    {
        public const int Width = 8;
        public const int Height = 5;

        private static readonly LevelTile[] GentleRamp =
        {
            new LevelTile(0, 4, TileKind.Rock),
            new LevelTile(1, 4, TileKind.Rock),
            new LevelTile(2, 3, TileKind.Spring),
            new LevelTile(3, 3, TileKind.Rock),
            new LevelTile(4, 2, TileKind.Rock),
            new LevelTile(5, 2, TileKind.Spring),
            new LevelTile(6, 1, TileKind.Rock),
            new LevelTile(7, 1, TileKind.Goal)
        };

        private static readonly LevelTile[] BrokenBridge =
        {
            new LevelTile(0, 3, TileKind.Rock),
            new LevelTile(1, 3, TileKind.Spring),
            new LevelTile(3, 1, TileKind.Rock),
            new LevelTile(4, 1, TileKind.Rock),
            new LevelTile(5, 3, TileKind.Spring),
            new LevelTile(6, 3, TileKind.Rock),
            new LevelTile(7, 3, TileKind.Goal)
        };

        private readonly TileKind[,] cells = new TileKind[Width, Height];

        public TileKind At(int x, int y) => cells[x, y];

        public static LevelBoard CreateDraft(LevelTemplate template)
        {
            LevelTile[] tiles = template switch
            {
                LevelTemplate.GentleRamp => GentleRamp,
                LevelTemplate.BrokenBridge => BrokenBridge,
                _ => throw new ArgumentOutOfRangeException(nameof(template))
            };

            if (!TryLoad(tiles, out LevelBoard board, out string error))
                throw new InvalidOperationException(error);

            return board;
        }

        public bool TrySet(int x, int y, TileKind kind)
        {
            if (!Contains(x, y) || kind < TileKind.Empty || kind > TileKind.Goal)
                return false;

            cells[x, y] = kind;
            return true;
        }

        public List<LevelTile> Snapshot()
        {
            var result = new List<LevelTile>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    TileKind kind = cells[x, y];
                    if (kind != TileKind.Empty)
                        result.Add(new LevelTile(x, y, kind));
                }
            }
            return result;
        }

        public static bool TryLoad(IEnumerable<LevelTile> tiles, out LevelBoard board, out string error)
        {
            board = null;
            if (tiles == null)
            {
                error = "The level has no tile list.";
                return false;
            }

            var candidate = new LevelBoard();
            foreach (LevelTile tile in tiles)
            {
                if (!Contains(tile.X, tile.Y))
                {
                    error = $"Tile ({tile.X}, {tile.Y}) is outside the {Width} × {Height} grid.";
                    return false;
                }
                if (candidate.At(tile.X, tile.Y) != TileKind.Empty)
                {
                    error = $"More than one tile occupies ({tile.X}, {tile.Y}).";
                    return false;
                }
                if (tile.Kind == TileKind.Empty || !candidate.TrySet(tile.X, tile.Y, tile.Kind))
                {
                    error = $"Unknown tile type at ({tile.X}, {tile.Y}).";
                    return false;
                }
            }
            board = candidate;
            error = null;
            return true;
        }

        private static bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
