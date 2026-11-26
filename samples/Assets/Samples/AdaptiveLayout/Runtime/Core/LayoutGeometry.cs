using System;

namespace GameplaySamples.AdaptiveLayout
{
    // Geometry uses a bottom-left origin, like Unity camera and screen coordinates.
    public readonly struct LayoutRect
    {
        public LayoutRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float Right => X + Width;
        public float Top => Y + Height;
        public float CenterX => X + Width * 0.5f;
        public float CenterY => Y + Height * 0.5f;

        internal bool HasArea =>
            IsFinite(X) && IsFinite(Y) && IsFinite(Width) && IsFinite(Height) &&
            IsFinite(Right) && IsFinite(Top) && Width > 0f && Height > 0f;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct LayoutResult
    {
        internal LayoutResult(LayoutRect uiAnchors, LayoutRect cameraBounds)
        {
            UiAnchors = uiAnchors;
            CameraBounds = cameraBounds;
        }

        // This is a normalized rectangle suitable for use as a stretched RectTransform.
        public LayoutRect UiAnchors { get; }
        public LayoutRect CameraBounds { get; }
        public float OrthographicSize => CameraBounds.Height * 0.5f;
    }

    public static class LayoutGeometry
    {
        public static bool TryCalculate(LayoutRect viewport, LayoutRect safeArea, LayoutRect world, out LayoutResult layout)
        {
            layout = default;
            if (!viewport.HasArea || !safeArea.HasArea || !world.HasArea)
                return false;

            float left = Math.Max(viewport.X, safeArea.X);
            float bottom = Math.Max(viewport.Y, safeArea.Y);
            float right = Math.Min(viewport.Right, safeArea.Right);
            float top = Math.Min(viewport.Top, safeArea.Top);
            if (left >= right || bottom >= top)
                return false;

            var anchors = new LayoutRect(
                (left - viewport.X) / viewport.Width,
                (bottom - viewport.Y) / viewport.Height,
                (right - left) / viewport.Width,
                (top - bottom) / viewport.Height);

            // The UI avoids cutouts; the camera uses the full viewport so the world can
            // extend behind them. Fit whichever axis would otherwise crop the world.
            float aspect = viewport.Width / viewport.Height;
            float height = Math.Max(world.Height, world.Width / aspect);
            float width = height * aspect;
            var camera = new LayoutRect(world.CenterX - width * 0.5f, world.CenterY - height * 0.5f,
                width, height);
            if (!camera.HasArea)
                return false;

            layout = new LayoutResult(anchors, camera);
            return true;
        }
    }

    public sealed class AdaptiveLayoutState
    {
        public bool HasLayout { get; private set; }
        public LayoutResult Current { get; private set; }

        public bool Update(LayoutRect viewport, LayoutRect safeArea, LayoutRect world)
        {
            // Resizing can briefly yield zero or empty geometry. Keep the last valid
            // transforms until a complete viewport is available again.
            if (!LayoutGeometry.TryCalculate(viewport, safeArea, world, out LayoutResult next))
                return false;

            Current = next;
            HasLayout = true;
            return true;
        }
    }
}
