using UnityEngine;

namespace GameplaySamples.LayeredLayout
{
    public sealed class ImportedLayerLayout : MonoBehaviour
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private PuzzleLayoutAsset puzzleData;

        public int Width => width;
        public int Height => height;
        public PuzzleLayoutAsset PuzzleData => puzzleData;

        public void SetLayout(PuzzleLayoutAsset layout)
        {
            puzzleData = layout;
            SetSize(layout.Width, layout.Height);
        }

        public void SetSize(int canvasWidth, int canvasHeight)
        {
            width = canvasWidth;
            height = canvasHeight;
        }
    }
}
