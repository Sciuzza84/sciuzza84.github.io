using UnityEngine;

namespace GameplaySamples.LayeredLayout
{
    public sealed class LayeredLayoutExplorer : MonoBehaviour
    {
        private static readonly Color Background = new Color32(17, 25, 29, 255);
        private static readonly Color Panel = new Color32(25, 42, 47, 255);
        private static readonly Color GridLine = new Color32(42, 60, 65, 255);
        private static readonly Color Text = new Color32(226, 237, 231, 255);
        private static readonly Color Muted = new Color32(171, 192, 187, 255);
        private static readonly Color Amber = new Color32(248, 185, 106, 255);
        private static readonly Color Mint = new Color32(119, 219, 182, 255);

        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle note;
        private GUIStyle statusStyle;
        private GUIStyle action;
        private GUIStyle orderMark;
        private LayerMap layout;
        private string status;
        private string acceptedSource;
        private bool rejected;
        private int selectedLayer;
        private Vector2 scroll;

        private void Awake()
        {
            Preview("Valid");
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            Fill(new Rect(0, 0, Screen.width, Screen.height), Background);
            float pageWidth = Screen.width - 16f;
            float width = Mathf.Min(1036f, pageWidth - 32f);
            float left = (pageWidth - width) * 0.5f;
            bool wide = width >= 740f;
            float statusTop = wide ? 150f : 207f;
            float statusHeight = wide ? 60f : 79f;
            float mapHeight = wide ? 355f : 262f;
            float dataHeight = Mathf.Max(213f, 92f + (layout?.Layers.Count ?? 0) * 35f);
            float cardsTop = statusTop + statusHeight + 14f;
            float dataTop = wide ? cardsTop : cardsTop + mapHeight + 14f;
            float notesTop = Mathf.Max(cardsTop + mapHeight, dataTop + dataHeight) + 12f;

            scroll = GUI.BeginScrollView(new Rect(0, 0, Screen.width, Screen.height), scroll,
                new Rect(0, 0, pageWidth, notesTop + 65f));
            GUI.Label(new Rect(left, 16, width, 42), "Layered layout", title);
            GUI.Label(new Rect(left, 63, width, 37), "Follow artwork positions into a puzzle prefab and its level data.", body);

            DrawActions(left, wide ? 95f : 108f, width, wide);
            var statusBox = new Rect(left, statusTop, width, statusHeight);
            Color statusColor = rejected ? Amber : Mint;
            Fill(statusBox, Panel);
            Fill(new Rect(left, statusTop, 4f, statusHeight), statusColor);
            statusStyle.normal.textColor = statusColor;
            GUI.Label(new Rect(left + 15f, statusTop + 8f, width - 30f, statusHeight - 13f), status, statusStyle);

            if (layout != null)
            {
                float mapWidth = wide ? width * 0.59f - 8f : width;
                float dataLeft = wide ? left + mapWidth + 14f : left;
                float dataWidth = wide ? width - mapWidth - 14f : width;
                DrawLayout(new Rect(left, cardsTop, mapWidth, mapHeight));
                DrawData(new Rect(dataLeft, dataTop, dataWidth, dataHeight));
            }
            GUI.Label(new Rect(left, notesTop, width, 49),
                "The Web preview uses bundled JSON. The Unity Editor writes the prefab and ScriptableObject.", note);
            GUI.EndScrollView();
        }

        private void DrawActions(float left, float top, float width, bool wide)
        {
            const float gap = 8f;
            const float height = 40f;
            int columns = wide ? 4 : 2;
            float buttonWidth = (width - gap * (columns - 1)) / columns;
            DrawAction(0, "Preview original", "Valid");
            DrawAction(1, "Move beacon", "Moved");
            DrawAction(2, "Try invalid name", "InvalidName");
            DrawAction(3, "Try invalid position", "Invalid");

            void DrawAction(int index, string label, string fixture)
            {
                var bounds = new Rect(left + index % columns * (buttonWidth + gap), top + index / columns * (height + gap),
                    buttonWidth, height);
                if (GUI.Button(bounds, label, action))
                    Preview(fixture);
            }
        }

        private void Preview(string name)
        {
            TextAsset source = Resources.Load<TextAsset>($"LayeredLayout/{name}");
            if (!source)
            {
                status = "The bundled JSON fixture is missing.";
                rejected = true;
                return;
            }

            if (LayerJson.TryRead(source.text, out LayerMap next, out string error))
            {
                layout = next;
                selectedLayer = name == "Moved" ? next.Layers.Count - 1 : 0;
                acceptedSource = name + ".json";
                rejected = false;
                status = $"Accepted {acceptedSource}. {next.Layers.Count} layers sorted by their source order.";
                return;
            }

            rejected = true;
            status = layout == null ? error : $"{error}\nStill showing {acceptedSource}.";
        }

        private void DrawLayout(Rect panel)
        {
            Fill(panel, Panel);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 12f, panel.width - 30f, 28f), "Artwork placement", heading);
            var field = new Rect(panel.x + 22f, panel.y + 49f, panel.width - 44f, panel.height - 84f);
            float scale = Mathf.Min(field.width / layout.Width, field.height / layout.Height);
            var canvas = new Rect(field.center.x - layout.Width * scale * 0.5f,
                field.center.y - layout.Height * scale * 0.5f, layout.Width * scale, layout.Height * scale);
            Fill(canvas, Background);
            for (int quarter = 1; quarter < 4; quarter++)
            {
                Fill(new Rect(canvas.x + canvas.width * quarter / 4f, canvas.y, 1f, canvas.height), GridLine);
                Fill(new Rect(canvas.x, canvas.y + canvas.height * quarter / 4f, canvas.width, 1f), GridLine);
            }

            for (int index = 0; index < layout.Layers.Count; index++)
            {
                ReadOnlyLayer layer = layout.Layers[index];
                float x = canvas.xMin + layer.PixelX * scale;
                float y = canvas.yMin + layer.PixelY * scale;
                var marker = new Rect(x - 13f, y - 13f, 26f, 26f);
                Fill(marker, index == selectedLayer ? Amber : Mint);
                GUI.Label(marker, layer.Order.ToString(), orderMark);
            }

            GUI.Label(new Rect(panel.x + 15f, panel.yMax - 31f, panel.width - 30f, 25f),
                $"{layout.Width} × {layout.Height} px · top-left origin", note);
        }

        private void DrawData(Rect panel)
        {
            Fill(panel, Panel);
            float width = panel.width - 30f;
            float left = panel.x + 15f;
            GUI.Label(new Rect(left, panel.y + 12f, width, 28f), "Level data", heading);
            GUI.Label(new Rect(left + 3f, panel.y + 43f, 45f, 23f), "Order", note);
            GUI.Label(new Rect(left + 53f, panel.y + 43f, width - 153f, 23f), "Layer", note);
            GUI.Label(new Rect(panel.xMax - 110f, panel.y + 43f, 97f, 23f), "Source pixels", note);
            for (int index = 0; index < layout.Layers.Count; index++)
            {
                ReadOnlyLayer layer = layout.Layers[index];
                float top = panel.y + 68f + index * 35f;
                var row = new Rect(left, top, width, 31f);
                Fill(row, index == selectedLayer ? GridLine : Background);
                Fill(new Rect(left, top, 3f, row.height), index == selectedLayer ? Amber : Mint);
                GUI.Label(new Rect(left + 10f, top + 4f, 38f, 24f), layer.Order.ToString(), body);
                GUI.Label(new Rect(left + 53f, top + 4f, width - 153f, 25f), layer.Name, body);
                GUI.Label(new Rect(row.xMax - 92f, top + 4f, 89f, 25f), $"({layer.PixelX}, {layer.PixelY})", body);
                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                    selectedLayer = index;
            }
            GUI.Label(new Rect(left, panel.yMax - 36f, width, 30f), "Select a row to find its marker.", note);
        }

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold };
            title.normal.textColor = Text;
            heading = new GUIStyle(title) { fontSize = 19 };
            body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            body.normal.textColor = Text;
            note = new GUIStyle(body) { fontSize = 14 };
            note.normal.textColor = Muted;
            statusStyle = new GUIStyle(body) { fontStyle = FontStyle.Bold };
            action = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            orderMark = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            orderMark.normal.textColor = Background;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
