using System;
using System.Collections.Generic;

namespace GameplaySamples.LayeredLayout
{
    [Serializable]
    public sealed class LayerDocument
    {
        public int Width;
        public int Height;
        public LayerItem[] Layers;
    }

    [Serializable]
    public sealed class LayerItem
    {
        public string Name;
        public int X;
        public int Y;
        public int Order;
    }

    public sealed class LayerMap
    {
        private const int MaxCanvasSize = 4096;
        private const int MaxLayerCount = 128;
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ReadOnlyLayer> Layers { get; }

        private LayerMap(int width, int height, ReadOnlyLayer[] layers)
        {
            Width = width;
            Height = height;
            Layers = Array.AsReadOnly(layers);
        }

        public static bool TryCreate(LayerDocument document, out LayerMap layout, out string error)
        {
            layout = null;
            if (document == null || document.Width < 1 || document.Height < 1 ||
                document.Width > MaxCanvasSize || document.Height > MaxCanvasSize)
            {
                error = "The canvas width and height must each be between 1 and 4096 pixels.";
                return false;
            }

            if (document.Layers == null || document.Layers.Length < 1 || document.Layers.Length > MaxLayerCount)
            {
                error = "Provide between 1 and 128 layers.";
                return false;
            }

            var names = new HashSet<string>(StringComparer.Ordinal);
            var orders = new HashSet<int>();
            var sorted = new ReadOnlyLayer[document.Layers.Length];
            for (int index = 0; index < sorted.Length; index++)
            {
                LayerItem layer = document.Layers[index];
                if (layer == null || !IsItemName(layer.Name))
                {
                    string name = string.IsNullOrEmpty(layer?.Name) ? "<missing>" : layer.Name;
                    error = $"Layer {index + 1} ('{name}') needs a name of the form item_something.";
                    return false;
                }
                if (!names.Add(layer.Name))
                {
                    error = $"Duplicate layer name: {layer.Name}.";
                    return false;
                }
                if (layer.Order < 1 || !orders.Add(layer.Order))
                {
                    error = $"{layer.Name} needs a positive, unique layer order.";
                    return false;
                }
                if (layer.X < 1 || layer.X > document.Width || layer.Y < 1 || layer.Y > document.Height)
                {
                    error = $"{layer.Name} at ({layer.X}, {layer.Y}) falls outside the canvas.";
                    return false;
                }

                sorted[index] = new ReadOnlyLayer(layer.Name, layer.X, layer.Y, layer.Order);
            }

            Array.Sort(sorted, (left, right) => left.Order.CompareTo(right.Order));
            layout = new LayerMap(document.Width, document.Height, sorted);
            error = null;
            return true;
        }

        private static bool IsItemName(string name)
        {
            const string prefix = "item_";
            if (name == null || name.Length <= prefix.Length || name.Length > 40 ||
                !name.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            for (int index = prefix.Length; index < name.Length; index++)
            {
                char character = name[index];
                if ((character < 'a' || character > 'z') && (character < '0' || character > '9'))
                    return false;
            }
            return true;
        }
    }

    public readonly struct ReadOnlyLayer
    {
        public string Name { get; }
        public int PixelX { get; }
        public int PixelY { get; }
        public int Order { get; }

        internal ReadOnlyLayer(string name, int pixelX, int pixelY, int order)
        {
            Name = name;
            PixelX = pixelX;
            PixelY = pixelY;
            Order = order;
        }
    }
}
