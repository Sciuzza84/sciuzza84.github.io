using System;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            new Entry("match-recovery", "MatchRecovery", "Match recovery", "Drop a delivery and recover a scoreboard replica from a snapshot.")
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
