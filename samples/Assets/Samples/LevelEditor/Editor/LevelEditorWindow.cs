using System;
using UnityEditor;
using UnityEngine;

namespace GameplaySamples.LevelEditor.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        private const string DraftPath = "Assets/Samples/LevelEditor/Generated/LevelDraft.asset";
        private static readonly string[] TemplateNames = { "Gentle Ramp", "Broken Bridge" };
        private static readonly string[] Tools = { "Erase", "Rock", "Spring", "Goal" };
        private static readonly GUIContent[] CellLabels =
        {
            new GUIContent("·", "Empty"), new GUIContent("R", "Rock"),
            new GUIContent("S", "Spring"), new GUIContent("G", "Goal")
        };
        private static readonly Color[] Colors =
        {
            new Color32(162, 169, 171, 255),
            new Color32(214, 210, 199, 255),
            new Color32(103, 214, 172, 255),
            new Color32(248, 185, 106, 255)
        };

        private LevelBoard board;
        private GUIStyle previewLabel;
        private Vector2 scroll;
        private int selectedTemplate;
        private int selected = 1;
        private bool dirty;
        private string status = "Copy a template or paint an empty draft.";

        [MenuItem("Samples/Open Level Editor")]
        private static void Open()
        {
            var panel = GetWindow<LevelEditorWindow>("Level Editor");
            panel.minSize = new Vector2(400, 360);
        }

        private void OnEnable()
        {
            if (board == null)
                board = new LevelBoard();
        }

        private void OnGUI()
        {
            using (var view = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = view.scrollPosition;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Template", GUILayout.Width(60));
                    selectedTemplate = EditorGUILayout.Popup(selectedTemplate, TemplateNames);
                    if (GUILayout.Button("Copy to Draft", GUILayout.Width(110)))
                        CopyTemplate();
                }
                EditorGUILayout.Space(8);

                if (EditorGUIUtility.currentViewWidth >= 640)
                {
                    float columnWidth = (EditorGUIUtility.currentViewWidth - 42f) / 2;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(columnWidth)))
                            DrawEditGrid();
                        GUILayout.Space(12);
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(columnWidth)))
                            DrawPreview();
                    }
                }
                else
                {
                    DrawEditGrid();
                    EditorGUILayout.Space(8);
                    DrawPreview();
                }

                EditorGUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Save Draft"))
                        SaveDraft();
                    if (GUILayout.Button("Reopen Draft"))
                        ReopenDraft(() => EditorUtility.DisplayDialog("Reopen Draft",
                            "Reopen the saved draft and discard unsaved changes?", "Reopen", "Keep Editing"));
                }
                string message = dirty ? $"Unsaved changes. {status}".TrimEnd() : status;
                EditorGUILayout.HelpBox(message, MessageType.None);
            }
        }

        private void DrawEditGrid()
        {
            EditorGUILayout.LabelField("Edit Draft", EditorStyles.boldLabel);
            selected = GUILayout.Toolbar(selected, Tools);
            EditorGUILayout.Space(4);

            Color previous = GUI.backgroundColor;
            for (int y = 0; y < LevelBoard.Height; y++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int x = 0; x < LevelBoard.Width; x++)
                    {
                        TileKind kind = board.At(x, y);
                        GUI.backgroundColor = Colors[(int)kind];
                        if (GUILayout.Button(CellLabels[(int)kind], GUILayout.Height(28), GUILayout.ExpandWidth(true)))
                            PaintTile(x, y, (TileKind)selected);
                    }
                }
            }
            GUI.backgroundColor = previous;
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
            Rect area = GUILayoutUtility.GetRect(1f, 174f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            previewLabel ??= new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color32(27, 35, 41, 255) }
            };
            EditorGUI.DrawRect(area, new Color32(27, 35, 41, 255));
            float tileSize = Mathf.Floor(Mathf.Min((area.width - 16f) / LevelBoard.Width,
                (area.height - 16f) / LevelBoard.Height));
            Vector2 origin = area.center - new Vector2(LevelBoard.Width, LevelBoard.Height) * (tileSize / 2);
            for (int y = 0; y < LevelBoard.Height; y++)
            {
                for (int x = 0; x < LevelBoard.Width; x++)
                {
                    TileKind kind = board.At(x, y);
                    var tile = new Rect(origin.x + x * tileSize + 1f, origin.y + y * tileSize + 1f,
                        tileSize - 2f, tileSize - 2f);
                    Color color = kind == TileKind.Empty ? new Color32(40, 50, 57, 255) : Colors[(int)kind];
                    EditorGUI.DrawRect(tile, color);
                    if (kind != TileKind.Empty)
                        GUI.Label(tile, CellLabels[(int)kind], previewLabel);
                }
            }
        }

        private void CopyTemplate()
        {
            if (dirty && !EditorUtility.DisplayDialog("Copy to Draft",
                    "Replace the current draft and discard unsaved changes?", "Copy Template", "Keep Editing"))
                return;

            board = LevelBoard.CreateDraft((LevelTemplate)selectedTemplate);
            dirty = true;
            status = $"Copied {TemplateNames[selectedTemplate]} to the draft.";
        }

        private void PaintTile(int x, int y, TileKind kind)
        {
            if (board.At(x, y) == kind || !board.TrySet(x, y, kind))
                return;

            dirty = true;
            status = string.Empty;
        }

        private void SaveDraft()
        {
            if (!LevelDraftAssets.TrySave(DraftPath, board, out status))
                return;

            status = "Draft saved. Reopen it to restore the same grid.";
            dirty = false;
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<LevelDraft>(DraftPath));
        }

        private void ReopenDraft(Func<bool> confirmDiscard)
        {
            if (!LevelDraftAssets.TryOpen(DraftPath, out LevelBoard loaded, out string error))
            {
                status = error;
                return;
            }
            if (dirty && !confirmDiscard())
                return;

            board = loaded;
            dirty = false;
            status = "Draft reopened.";
        }
    }
}
