using UnityEngine;

namespace GameplaySamples.AdaptiveLayout
{
    public static class UnityLayoutAdapter
    {
        // The safe root must be a direct child of a canvas that fills the viewport.
        // The camera should look straight down the Z axis at the XY gameplay plane.
        public static bool TryApply(Camera camera, RectTransform safeRoot, Rect worldBounds)
        {
            return camera && TryApply(camera, safeRoot, worldBounds, camera.pixelRect, Screen.safeArea);
        }

        internal static bool TryApply(Camera camera, RectTransform safeRoot, Rect worldBounds, Rect viewport, Rect safeArea)
        {
            if (!camera || !safeRoot || !camera.orthographic ||
                !LayoutGeometry.TryCalculate(Convert(viewport), Convert(safeArea), Convert(worldBounds), out LayoutResult layout))
                return false;

            LayoutRect anchors = layout.UiAnchors;
            safeRoot.anchorMin = new Vector2(anchors.X, anchors.Y);
            safeRoot.anchorMax = new Vector2(anchors.Right, anchors.Top);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            camera.orthographicSize = layout.OrthographicSize;
            Vector3 cameraPosition = camera.transform.position;
            cameraPosition.x = layout.CameraBounds.CenterX;
            cameraPosition.y = layout.CameraBounds.CenterY;
            camera.transform.position = cameraPosition;
            return true;
        }

        private static LayoutRect Convert(Rect rect) => new LayoutRect(rect.x, rect.y, rect.width, rect.height);
    }
}
