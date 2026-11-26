using UnityEngine;

namespace GameplaySamples.ContentUpdates
{
    public sealed class ContentUpdateExplorer : MonoBehaviour
    {
        private const float StackedControlsWidth = 520f;
        private static readonly string[] PlatformLabels = { "Android", "iOS" };
        private static readonly string[] VersionLabels = { "Version 1", "Version 2" };
        private static readonly string[] FaultLabels = { "None", "Corrupt file", "Cancel after download" };
        private static readonly Color Ink = new Color32(235, 243, 235, 255);
        private static readonly Color Muted = new Color32(166, 192, 187, 255);
        private static readonly Color Accent = new Color32(119, 222, 183, 255);
        private static readonly Color Amber = new Color32(244, 187, 113, 255);

        private readonly InstalledContent[] installations = new InstalledContent[2];
        private readonly ContentFixture[,] candidates = new ContentFixture[2, 2];
        private int platformIndex;
        private int versionIndex = 1;
        private int faultIndex;
        private string status = "Version 1 is installed. Try applying version 2.";
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle text;
        private GUIStyle signal;
        private GUIStyle action;

        private void Awake()
        {
            for (int index = 0; index < 2; index++)
            {
                var platform = (ContentPlatform)index;
                ContentFixture first = ContentFixture.Create(platform, 1);
                candidates[index, 0] = first;
                candidates[index, 1] = ContentFixture.Create(platform, 2);
                installations[index] = InstalledContent.Update(InstalledContent.Empty(platform), first.Manifest,
                    first.Fetch, () => false).Installed;
            }
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1020f, Screen.height / 710f), 1f, 1.6f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            float panel = Mathf.Min(864f, width - 32f);
            bool stackControls = panel < StackedControlsWidth;
            GUILayout.BeginArea(new Rect((width - panel) * 0.5f, 14f, panel, height - 28f));
            if (stackControls)
            {
                scroll = GUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
                float contentWidth = panel - GUI.skin.verticalScrollbar.fixedWidth - 8f;
                GUILayout.BeginVertical(GUILayout.Width(contentWidth));
            }
            else
            {
                scroll = GUILayout.BeginScrollView(scroll);
            }

            GUILayout.Label("Content updates", title);
            GUILayout.Label("Install a versioned manifest. Transfer only changed files and keep the last working snapshot if staging fails.", text);
            GUILayout.Space(18f);
            SelectOptions(stackControls);
            DrawManifest();
            GUILayout.Space(12f);
            if (GUILayout.Button("Apply update", action, GUILayout.Height(42f)))
                Apply();
            if (GUILayout.Button("Clear this installation", action, GUILayout.Height(34f)))
            {
                installations[platformIndex] = InstalledContent.Empty((ContentPlatform)platformIndex);
                status = "Installation cleared. Apply version 1 to transfer all three files.";
            }
            GUILayout.Space(12f);
            GUILayout.Label(status, signal);
            GUILayout.Label("Simulated transport and storage. Files are synthetic.", text);

            if (stackControls)
                GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = previous;
        }

        private void SelectOptions(bool stackControls)
        {
            GUILayout.Label("Platform", heading);
            int selectedPlatform = SelectOption(platformIndex, PlatformLabels, 34f, stackControls);
            if (selectedPlatform != platformIndex)
            {
                platformIndex = selectedPlatform;
                status = "Platform switched. Each installation keeps its own copy.";
            }
            GUILayout.Label("Manifest", heading);
            versionIndex = SelectOption(versionIndex, VersionLabels, 34f, stackControls);
            GUILayout.Label("Simulate a failure", heading);
            faultIndex = SelectOption(faultIndex, FaultLabels, 42f, stackControls);
            GUILayout.Space(13f);
        }

        private int SelectOption(int selected, string[] options, float rowHeight, bool stackControls)
        {
            int columns = stackControls ? 1 : options.Length;
            int rows = stackControls ? options.Length : 1;
            return GUILayout.SelectionGrid(selected, options, columns, action, GUILayout.Height(rowHeight * rows));
        }

        private void DrawManifest()
        {
            InstalledContent installed = installations[platformIndex];
            ContentManifest candidate = candidates[platformIndex, versionIndex].Manifest;
            string installedVersion = installed.Version == 0 ? "empty" : "v" + installed.Version;
            GUILayout.Label($"Installed: {installedVersion}  |  Candidate: v{candidate.Version}", heading);

            foreach (ContentEntry file in candidate.Files)
            {
                bool reuse = installed.Contains(file);
                signal.normal.textColor = reuse ? Accent : Amber;
                GUILayout.Label(file.Name + "   " + (reuse ? "Reuse" : "Download"), signal);
            }
            signal.normal.textColor = Accent;
        }

        private void Apply()
        {
            ContentFixture fixture = candidates[platformIndex, versionIndex];
            bool transferred = false;
            UpdateResult result = InstalledContent.Update(installations[platformIndex], fixture.Manifest,
                name =>
                {
                    transferred = true;
                    string payload = fixture.Fetch(name);
                    return faultIndex == 1 ? payload + " (corrupted)" : payload;
                },
                () => faultIndex == 2 && transferred);
            installations[platformIndex] = result.Installed;

            switch (result.Outcome)
            {
                case UpdateOutcome.Applied:
                    status = $"Applied v{result.Installed.Version}. Transferred {result.Downloaded}, reused {result.Reused}.";
                    break;
                case UpdateOutcome.InvalidPayload:
                    status = $"Integrity check failed for {result.FailedFile}. Kept the installed snapshot.";
                    break;
                case UpdateOutcome.Cancelled:
                    status = $"Cancelled after {result.Downloaded} transfer. Kept the installed snapshot.";
                    break;
            }
        }

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            text = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            signal = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            action = new GUIStyle(GUI.skin.button) { fontSize = 14, wordWrap = true };
            title.normal.textColor = Ink;
            heading.normal.textColor = Ink;
            text.normal.textColor = Muted;
            signal.normal.textColor = Accent;
        }
    }
}
