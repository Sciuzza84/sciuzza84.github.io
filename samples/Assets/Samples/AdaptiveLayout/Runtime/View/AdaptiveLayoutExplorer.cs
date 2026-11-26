using UnityEngine;

namespace GameplaySamples.AdaptiveLayout
{
    public sealed class AdaptiveLayoutExplorer : MonoBehaviour
    {
        private enum Preset { PhonePortrait, PhoneLandscape, Wide, Tablet }

        [SerializeField] private Preset startingPreset = Preset.PhonePortrait;
        [SerializeField] private bool startWithCutouts = true;

        private static readonly string[] PresetNames = { "9:16", "19.5:9", "16:9", "4:3" };
        private static readonly LayoutRect World = new LayoutRect(-6f, -3.5f, 12f, 7f);
        private static readonly Color Ink = Rgb(0x111C21);
        private static readonly Color Panel = Rgb(0x1B2A2F);
        private static readonly Color Grid = Rgb(0x2B4044);
        private static readonly Color Edge = Rgb(0x476467);
        private static readonly Color Paper = Rgb(0xECECE2);
        private static readonly Color Muted = Rgb(0xAEBCB9);
        private static readonly Color Mint = Rgb(0x7BDFBB);
        private static readonly Color Amber = Rgb(0xF2B973);
        private static readonly Color Cutout = new Color(0.42f, 0.22f, 0.1f, 0.8f);

        private readonly AdaptiveLayoutState state = new AdaptiveLayoutState();
        private Vector2 scroll;
        private LayoutRect viewport;
        private Preset preset;
        private bool cutouts;
        private bool adaptive = true;
        private GUIStyle title, heading, label, small, badge, normalButton, selectedButton;
        private Texture2D normalTexture, selectedTexture, hoverTexture;

        private void Awake()
        {
            preset = startingPreset;
            cutouts = startWithCutouts;
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            viewport = ViewportFor(preset);
            LayoutRect safe = viewport;
            if (cutouts)
            {
                // Insets are normalized for simulation so narrower previews keep
                // the same safe-area proportions instead of a fixed device pixel count.
                safe = viewport.Height > viewport.Width
                    ? new LayoutRect(viewport.Width * 0.025f, viewport.Height * 0.07f,
                        viewport.Width * 0.95f, viewport.Height * 0.81f)
                    : new LayoutRect(viewport.Width * 0.11f, viewport.Height * 0.025f,
                        viewport.Width * 0.84f, viewport.Height * 0.91f);
            }
            state.Update(viewport, safe, World);
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 980f, Screen.height / 640f), 1f, 2f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            bool stacked = width < 710f;
            float margin = stacked ? 12f : 24f;
            const float top = 88f;
            const float controlsHeight = 282f;
            float previewHeight = stacked ? Mathf.Clamp(height * 0.65f, 325f, 520f) : Mathf.Max(400f, height - 112f);
            float contentHeight = top + previewHeight + margin + (stacked ? controlsHeight + 12f : 0f);
            Matrix4x4 savedMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Fill(new Rect(0, 0, width, height), Ink);

            bool scrollable = contentHeight > height;
            if (scrollable)
            {
                width -= 16f;
                scroll = GUI.BeginScrollView(new Rect(0, 0, width + 16f, height), scroll,
                    new Rect(0, 0, width, contentHeight), false, false);
            }

            float contentWidth = Mathf.Min(1320f, width - margin * 2f);
            float x = (width - contentWidth) * 0.5f;
            GUI.Label(new Rect(x, 10f, contentWidth, 39f), "Adaptive layout", title);
            GUI.Label(new Rect(x, 52f, contentWidth, 25f), "Fit the game world. Keep the HUD in the safe area.", small);

            if (stacked)
            {
                DrawPreview(new Rect(x, top, contentWidth, previewHeight));
                DrawControls(new Rect(x, top + previewHeight + 12f, contentWidth, controlsHeight));
            }
            else
            {
                const float rail = 242f;
                DrawControls(new Rect(x, top, rail, controlsHeight));
                DrawPreview(new Rect(x + rail + 14f, top, contentWidth - rail - 14f, previewHeight));
            }

            if (scrollable)
                GUI.EndScrollView();
            GUI.matrix = savedMatrix;
        }

        private void DrawControls(Rect box)
        {
            GUI.BeginGroup(box);
            Fill(new Rect(0f, 0f, box.width, box.height), Panel);
            GUI.Label(new Rect(14f, 9f, box.width - 28f, 25f), "Viewport aspect", heading);
            float buttonWidth = (box.width - 34f) * 0.5f;
            for (int i = 0; i < PresetNames.Length; i++)
            {
                var option = (Preset)i;
                float x = 14f + i % 2 * (buttonWidth + 6f);
                float y = 42f + i / 2 * 42f;
                if (Button(new Rect(x, y, buttonWidth, 36f), PresetNames[i], preset == option))
                {
                    preset = option;
                    UpdateLayout();
                }
            }

            float rowWidth = box.width - 28f;
            if (Button(new Rect(14f, 136f, rowWidth, 38f), cutouts ? "Cutouts on" : "Cutouts off", cutouts))
            {
                cutouts = !cutouts;
                UpdateLayout();
            }
            if (Button(new Rect(14f, 183f, rowWidth, 38f), adaptive ? "Adaptation on" : "Adaptation off", adaptive))
                adaptive = !adaptive;

            GUI.Label(new Rect(14f, 229f, rowWidth, 40f),
                "Turn adaptation off to see the fixed camera and full-screen HUD.", small);
            GUI.EndGroup();
        }

        private void DrawPreview(Rect box)
        {
            GUI.BeginGroup(box);
            Fill(new Rect(0f, 0f, box.width, box.height), Panel);
            GUI.Label(new Rect(14f, 7f, box.width - 28f, 30f), "Simulated device", heading);

            Rect space = new Rect(14f, 45f, box.width - 28f, box.height - 93f);
            Fill(space, Ink);
            float aspect = viewport.Width / viewport.Height;
            float height = Mathf.Min(space.height - 24f, (space.width - 24f) / aspect);
            float width = height * aspect;
            Rect frame = new Rect(space.center.x - width * 0.5f, space.center.y - height * 0.5f, width, height);
            Outline(new Rect(frame.x - 3f, frame.y - 3f, frame.width + 6f, frame.height + 6f), Edge, 2f);
            DrawDevice(frame, aspect);

            float legendY = box.height - 37f;
            Fill(new Rect(14f, legendY + 7f, 8f, 8f), Mint);
            GUI.Label(new Rect(27f, legendY, 90f, 23f), "Safe UI", label);
            Fill(new Rect(117f, legendY + 7f, 8f, 8f), Amber);
            GUI.Label(new Rect(130f, legendY, box.width - 144f, 23f), "World bounds", label);
            GUI.EndGroup();
        }

        private void DrawDevice(Rect frame, float aspect)
        {
            if (!state.HasLayout)
                return;

            LayoutResult result = state.Current;
            LayoutRect camera = adaptive ? result.CameraBounds : FixedCamera(aspect);
            var safe = Project(result.UiAnchors, frame.width, frame.height);
            Rect ui = adaptive ? safe : new Rect(0f, 0f, frame.width, frame.height);

            GUI.BeginGroup(frame);
            Fill(new Rect(0f, 0f, frame.width, frame.height), Grid);
            DrawGround(camera, frame.size);
            DrawHud(ui);
            if (cutouts)
            {
                Fill(new Rect(0f, 0f, frame.width, safe.yMin), Cutout);
                Fill(new Rect(0f, safe.yMax, frame.width, frame.height - safe.yMax), Cutout);
                Fill(new Rect(0f, safe.yMin, safe.xMin, safe.height), Cutout);
                Fill(new Rect(safe.xMax, safe.yMin, frame.width - safe.xMax, safe.height), Cutout);
            }
            Outline(safe, Mint, 1f);
            GUI.EndGroup();
        }

        private static LayoutRect FixedCamera(float aspect)
        {
            float width = World.Height * aspect;
            return new LayoutRect(World.CenterX - width * 0.5f, World.Y, width, World.Height);
        }

        private static void DrawGround(LayoutRect camera, Vector2 size)
        {
            Rect ground = ProjectWorld(World, camera, size);
            Fill(ground, Rgb(0x263D3B));
            for (int x = -5; x <= 5; x++)
            {
                Rect line = ProjectWorld(new LayoutRect(x, -3.5f, 0.018f, 7f), camera, size);
                Fill(line, Edge);
            }
            for (int y = -3; y <= 3; y++)
            {
                Rect line = ProjectWorld(new LayoutRect(-6f, y, 12f, 0.018f), camera, size);
                Fill(line, Edge);
            }
            Fill(ProjectWorld(new LayoutRect(-5.2f, -0.5f, 1f, 1f), camera, size), Amber);
            Fill(ProjectWorld(new LayoutRect(4.2f, -0.5f, 1f, 1f), camera, size), Amber);
            Fill(ProjectWorld(new LayoutRect(-0.5f, -0.5f, 1f, 1f), camera, size), Mint);
            Outline(ground, Amber, 2f);
        }

        private void DrawHud(Rect area)
        {
            float padding = Mathf.Clamp(area.width * 0.055f, 3f, 9f);
            float size = Mathf.Clamp(area.width * 0.19f, 16f, 35f);
            var score = new Rect(area.xMin + padding, area.yMin + padding, size, 19f);
            var map = new Rect(area.xMax - padding - size, area.yMin + padding, size, 19f);
            var inventory = new Rect(area.center.x - size * 0.5f, area.yMax - padding - 19f, size, 19f);
            Fill(score, Mint);
            Fill(map, Mint);
            Fill(inventory, Mint);
            GUI.Label(score, "03", badge);
            GUI.Label(map, "M", badge);
            GUI.Label(inventory, "I", badge);
        }

        private static LayoutRect ViewportFor(Preset preset)
        {
            switch (preset)
            {
                case Preset.PhoneLandscape: return new LayoutRect(0f, 0f, 1950f, 900f);
                case Preset.Wide: return new LayoutRect(0f, 0f, 1600f, 900f);
                case Preset.Tablet: return new LayoutRect(0f, 0f, 1200f, 900f);
                default: return new LayoutRect(0f, 0f, 900f, 1600f);
            }
        }

        private static Rect ProjectWorld(LayoutRect rect, LayoutRect camera, Vector2 size)
        {
            var normalized = new LayoutRect((rect.X - camera.X) / camera.Width,
                (rect.Y - camera.Y) / camera.Height, rect.Width / camera.Width, rect.Height / camera.Height);
            return Project(normalized, size.x, size.y);
        }

        private static Rect Project(LayoutRect rect, float width, float height)
        {
            return new Rect(rect.X * width, (1f - rect.Top) * height, rect.Width * width, rect.Height * height);
        }

        private bool Button(Rect rect, string text, bool active)
        {
            return GUI.Button(rect, text, active ? selectedButton : normalButton);
        }

        private static void Outline(Rect rect, Color color, float thickness)
        {
            Fill(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            Fill(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            Fill(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private static void Fill(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint && rect.width > 0f && rect.height > 0f)
                GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        private void CreateStyles()
        {
            title = Text(31, Paper, true);
            heading = Text(18, Paper, true);
            label = Text(14, Muted);
            small = Text(13, Muted);
            small.wordWrap = true;
            badge = Text(11, Ink, true, TextAnchor.MiddleCenter);
            normalTexture = Solid(Grid);
            selectedTexture = Solid(Mint);
            hoverTexture = Solid(Edge);
            normalButton = Text(15, Paper, true, TextAnchor.MiddleCenter);
            normalButton.normal.background = normalTexture;
            normalButton.hover.background = normalButton.focused.background = normalButton.active.background = hoverTexture;
            selectedButton = Text(15, Ink, true, TextAnchor.MiddleCenter);
            selectedButton.normal.background = selectedButton.hover.background = selectedButton.focused.background =
                selectedButton.active.background = selectedTexture;
        }

        private static GUIStyle Text(int size, Color color, bool bold = false, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
                alignment = align };
            style.normal.textColor = style.hover.textColor = style.focused.textColor = style.active.textColor = color;
            return style;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static Color Rgb(int hex) => new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);

        private void OnDestroy()
        {
            if (normalTexture) Destroy(normalTexture);
            if (selectedTexture) Destroy(selectedTexture);
            if (hoverTexture) Destroy(hoverTexture);
        }
    }
}
