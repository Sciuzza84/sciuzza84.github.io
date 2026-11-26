using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GameplaySamples.AdaptiveLayout.Editor
{
    public static class AdaptiveLayoutVerification
    {
        public static void VerifySample()
        {
            GameObject scene = new GameObject("Adaptive Layout Check");
            try
            {
                var cameraObject = new GameObject("Camera", typeof(Camera));
                cameraObject.transform.SetParent(scene.transform);
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;

                var canvasObject = new GameObject("Viewport Canvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(scene.transform);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                var root = new GameObject("Safe Root", typeof(RectTransform)).GetComponent<RectTransform>();
                root.SetParent(canvas.transform, false);

                var controller = scene.AddComponent<AdaptiveLayoutController>();
                var configuration = new SerializedObject(controller);
                configuration.FindProperty("targetCamera").objectReferenceValue = camera;
                configuration.FindProperty("safeRoot").objectReferenceValue = root;
                configuration.FindProperty("worldBounds").rectValue = new Rect(-2f, 3f, 12f, 7f);
                configuration.ApplyModifiedPropertiesWithoutUndo();

                // Inject pixels so this check exercises real components without a display or device.
                MethodInfo refresh = typeof(AdaptiveLayoutController).GetMethod("RefreshIfChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                bool Measure(Rect viewport, Rect safeArea) => (bool)refresh.Invoke(controller, new object[] { viewport, safeArea });

                var portrait = new Rect(10f, 30f, 900f, 1600f);
                var safe = new Rect(46f, 85f, 828f, 1490f);
                Check(Measure(portrait, safe), "The configured components did not accept the portrait sample.");
                Check(root.anchorMin == new Vector2(0.04f, 0.034375f) && root.anchorMax == new Vector2(0.96f, 0.965625f),
                    "The safe root did not receive the expected viewport-relative anchors.");
                Check(root.offsetMin == Vector2.zero && root.offsetMax == Vector2.zero, "The root offsets were not cleared.");
                Check(camera.transform.position == new Vector3(4f, 6.5f, -10f) && Mathf.Approximately(camera.orthographicSize, 10.666667f),
                    "The camera did not frame the world while retaining its depth.");

                Vector2 initialMin = root.anchorMin;
                Vector2 initialMax = root.anchorMax;
                float initialSize = camera.orthographicSize;
                Check(!Measure(new Rect(10f, 30f, 0f, 1600f), safe), "An empty viewport was applied.");
                Check(root.anchorMin == initialMin && root.anchorMax == initialMax && camera.orthographicSize == initialSize,
                    "An invalid measurement changed the last valid transforms.");

                var landscape = new Rect(0f, 0f, 1600f, 900f);
                Check(Measure(landscape, landscape) && Mathf.Approximately(camera.orthographicSize, 3.5f), "Landscape did not fit the world.");
                Check(Measure(portrait, safe) && camera.orthographicSize == initialSize && root.anchorMin == initialMin && root.anchorMax == initialMax,
                    "Portrait was not restored after the landscape measurement.");

                camera.orthographicSize = initialSize + 1f;
                Check(!Measure(portrait, safe) && camera.orthographicSize == initialSize + 1f,
                    "Identical measurements rewrote the camera.");

                Check(Measure(portrait, portrait) && root.anchorMin == Vector2.zero && root.anchorMax == Vector2.one,
                    "A safe-area-only change did not refresh the anchors.");
                configuration.Update();
                configuration.FindProperty("worldBounds").rectValue = new Rect(-3f, -2f, 6f, 4f);
                configuration.ApplyModifiedPropertiesWithoutUndo();
                Check(Measure(portrait, portrait) && camera.transform.position == new Vector3(0f, 0f, -10f),
                    "A world-bounds-only change did not center the camera.");

                Debug.Log("Adaptive Layout Unity PASS: serialized setup, fit, anchors, invalid sample, rotation, unchanged input and independent changes.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scene);
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
