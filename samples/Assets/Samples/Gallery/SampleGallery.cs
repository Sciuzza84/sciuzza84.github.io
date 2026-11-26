using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
#endif

namespace GameplaySamples
{
    public sealed class SampleGallery : MonoBehaviour
    {
        private static readonly Entry[] Entries =
        {
            new Entry("evm-lite", "EvmLite", "EVM Lite", "Follow an event through additive scene loads and unloads."),
            new Entry("adaptive-layout", "AdaptiveLayout", "Adaptive layout", "Fit an HUD and a game board across several screen shapes."),
            new Entry("dungeon", "Dungeon", "Dungeon generator", "Reproduce a seed and see how side paths fit."),
            new Entry("combat", "Combat", "Turn-based combat", "Step through card effects and an opponent's choices."),
            new Entry("ar-placement", "ArPlacement", "AR placement", "Try target setup with simulated sensor inputs."),
            new Entry("content-updates", "ContentUpdates", "Content updates", "Apply versioned content and replay an interrupted update."),
            new Entry("layered-layout", "LayeredLayout", "Layered layout", "Validate an artist's layout before it becomes a Unity level."),
            new Entry("round-planner", "RoundPlanner", "Round planner", "Balance target exposure across a finite catalog."),
            new Entry("match-recovery", "MatchRecovery", "Match recovery", "Drop a delivery and recover a scoreboard replica from a snapshot."),
            new Entry("launch-handoff", "LaunchHandoff", "Launch handoff", "Compare two releases from guided motion to Rigidbody physics.")
        };

        private GUIStyle title;
        private GUIStyle body;
        private GUIStyle action;
        private Vector2 scroll;

        private void Start()
        {
            if (!Uri.TryCreate(Application.absoluteURL, UriKind.Absolute, out Uri address))
                return;

            foreach (string parameter in address.Query.TrimStart('?').Split('&'))
            {
                if (!parameter.StartsWith("sample=", StringComparison.Ordinal))
                    continue;

                string key = Uri.UnescapeDataString(parameter.Substring("sample=".Length));
                foreach (Entry entry in Entries)
                {
                    if (key == entry.Key && IsAvailable(entry))
                    {
                        SceneManager.LoadScene(entry.Scene);
                        return;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            int width = Mathf.Min(722, Screen.width - 40);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 24, width, Screen.height - 48));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Unity gameplay samples", title);
            GUILayout.Label("Interactive C# studies from Cristiano Piromalli’s work.", body);
            GUILayout.Space(26);
            foreach (Entry entry in Entries)
            {
                if (!IsAvailable(entry))
                    continue;

                if (GUILayout.Button(entry.Title, action, GUILayout.Height(43)))
                    SceneManager.LoadScene(entry.Scene);
                GUILayout.Label(entry.Description, body);
                GUILayout.Space(16);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static bool IsAvailable(Entry entry) => Application.CanStreamedLevelBeLoaded(entry.Scene);

#if UNITY_WEBGL && !UNITY_EDITOR
        private static int sceneRevision;

        [DllImport("__Internal")]
        private static extern void SetUnityPortfolioActiveSample(string key, string title);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ObserveActiveScene()
        {
            // Keep the portfolio navigation keyboard-accessible while the canvas is unfocused.
            WebGLInput.captureAllKeyboardInput = false;
            SceneManager.activeSceneChanged += PublishActiveScene;
            PublishActiveScene(default, SceneManager.GetActiveScene());
        }

        private static async void PublishActiveScene(Scene previous, Scene current)
        {
            int revision = ++sceneRevision;
            SetUnityPortfolioActiveSample(string.Empty, string.Empty);
            if (!TryDescribeScene(current.name, out string key, out string sceneTitle))
                return;

            // The first scene can already be active while the startup splash is still visible.
            do
            {
                await Awaitable.NextFrameAsync();
                if (revision != sceneRevision)
                    return;
            }
            while (!current.isLoaded || !SplashScreen.isFinished);

            await Awaitable.EndOfFrameAsync();
            if (revision == sceneRevision)
                SetUnityPortfolioActiveSample(key, sceneTitle);
        }

        private static bool TryDescribeScene(string scene, out string key, out string sceneTitle)
        {
            if (scene == "Samples")
            {
                key = "gallery";
                sceneTitle = "Unity gameplay samples";
                return true;
            }

            foreach (Entry entry in Entries)
            {
                if (entry.Scene == scene)
                {
                    key = entry.Key;
                    sceneTitle = entry.Title;
                    return true;
                }
            }

            key = sceneTitle = null;
            return false;
        }
#endif

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = true };
            title.normal.textColor = new Color32(237, 239, 225, 255);
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            body.normal.textColor = new Color32(176, 195, 192, 255);
            action = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            action.padding.left = 14;
        }

        private readonly struct Entry
        {
            public readonly string Key;
            public readonly string Scene;
            public readonly string Title;
            public readonly string Description;

            public Entry(string key, string scene, string title, string description)
            {
                Key = key;
                Scene = scene;
                Title = title;
                Description = description;
            }
        }
    }
}
