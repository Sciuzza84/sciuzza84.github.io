using System.Collections.Generic;
using UnityEngine;

namespace GameplaySamples.LevelEditor
{
    public sealed class LevelDraft : ScriptableObject
    {
        [SerializeField] private List<LevelTile> tiles = new List<LevelTile>();

        public bool TryRead(out LevelBoard board, out string error) => LevelBoard.TryLoad(tiles, out board, out error);

        public void SetBoard(LevelBoard board)
        {
            tiles = board.Snapshot();
        }
    }
}
