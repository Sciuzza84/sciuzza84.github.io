using UnityEngine;

namespace GameplaySamples.AdaptiveLayout
{
    [DisallowMultipleComponent]
    public sealed class AdaptiveLayoutController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RectTransform safeRoot;
        [SerializeField] private Rect worldBounds = new Rect(-6f, -3.5f, 12f, 7f);

        private Camera lastCamera;
        private RectTransform lastRoot;
        private Rect lastViewport;
        private Rect lastSafeArea;
        private Rect lastWorldBounds;
        private bool lastOrthographic;
        private bool hasMeasurement;

        private void OnEnable()
        {
            hasMeasurement = false;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            RefreshIfChanged(targetCamera ? targetCamera.pixelRect : default, Screen.safeArea);
        }

        private bool RefreshIfChanged(Rect viewport, Rect safeArea)
        {
            if (!targetCamera || !safeRoot)
            {
                hasMeasurement = false;
                return false;
            }

            bool orthographic = targetCamera.orthographic;
            if (hasMeasurement && lastCamera == targetCamera && lastRoot == safeRoot &&
                lastOrthographic == orthographic && lastViewport.Equals(viewport) &&
                lastSafeArea.Equals(safeArea) && lastWorldBounds.Equals(worldBounds))
                return false;

            // An invalid sample changes what we observed, but never mutates the last valid transforms.
            hasMeasurement = true;
            lastCamera = targetCamera;
            lastRoot = safeRoot;
            lastOrthographic = orthographic;
            lastViewport = viewport;
            lastSafeArea = safeArea;
            lastWorldBounds = worldBounds;
            return UnityLayoutAdapter.TryApply(targetCamera, safeRoot, worldBounds, viewport, safeArea);
        }
    }
}
