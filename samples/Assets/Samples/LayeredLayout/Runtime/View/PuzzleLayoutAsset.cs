using System;
using UnityEngine;

namespace GameplaySamples.LayeredLayout
{
    public sealed class PuzzleLayoutAsset : ScriptableObject
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private PuzzleItem[] items = Array.Empty<PuzzleItem>();

        public int Width => width;
        public int Height => height;
        public int ItemCount => items.Length;
        public PuzzleItem GetItem(int index) => items[index];

        public void SetLayout(LayerMap layout)
        {
            width = layout.Width;
            height = layout.Height;
            items = new PuzzleItem[layout.Layers.Count];
            for (int index = 0; index < items.Length; index++)
                items[index] = new PuzzleItem(layout.Layers[index]);
        }

        public bool Matches(LayerMap layout)
        {
            if (width != layout.Width || height != layout.Height || items.Length != layout.Layers.Count)
                return false;

            for (int index = 0; index < items.Length; index++)
            {
                ReadOnlyLayer layer = layout.Layers[index];
                PuzzleItem item = items[index];
                if (item.Name != layer.Name || item.PixelX != layer.PixelX || item.PixelY != layer.PixelY ||
                    item.Order != layer.Order)
                    return false;
            }

            return true;
        }

        [Serializable]
        public struct PuzzleItem
        {
            [SerializeField] private string name;
            [SerializeField] private int pixelX;
            [SerializeField] private int pixelY;
            [SerializeField] private int order;

            public string Name => name;
            public int PixelX => pixelX;
            public int PixelY => pixelY;
            public int Order => order;

            internal PuzzleItem(ReadOnlyLayer layer)
            {
                name = layer.Name;
                pixelX = layer.PixelX;
                pixelY = layer.PixelY;
                order = layer.Order;
            }
        }
    }
}
