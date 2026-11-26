using UnityEngine;

namespace GameplaySamples.ArPlacement
{
    public sealed class ArPlacementExplorer : MonoBehaviour
    {
        private static readonly GUIContent Caption = new GUIContent(
            "Simulated camera quality and device tilt. Tune the signal, then confirm the anchor.");
        private static readonly Color Background = new Color32(17, 28, 33, 255);
        private static readonly Color Panel = new Color32(25, 42, 47, 255);
        private static readonly Color Grid = new Color32(58, 76, 79, 255);
        private static readonly Color Ink = new Color32(238, 239, 229, 255);
        private static readonly Color Muted = new Color32(177, 197, 195, 255);
        private static readonly Color Mint = new Color32(123, 223, 187, 255);
        private static readonly Color Amber = new Color32(242, 185, 115, 255);

        private readonly PlacementGate gate = new PlacementGate();
        private float quality = 0.86f;
        private float tiltDegrees = 18f;
        private float separation = 2.4f;
        private float objectScale = 1f;
        private float replaySeconds;
        private bool replaying;
        private bool replayFinished;
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle hint;
        private GUIStyle button;

        private void Update()
        {
            // A suspended browser tab doesn't provide observed good frames.
            float seconds = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (replaying)
                AdvanceReplay(seconds);
            gate.Update(quality, tiltDegrees, seconds);
        }

        private void AdvanceReplay(float seconds)
        {
            replaySeconds += seconds;
            tiltDegrees = replaySeconds >= 0.9f && replaySeconds < 1.5f ? 47f : 18f;
            if (replaySeconds >= 3.25f)
            {
                replaying = false;
                replayFinished = true;
            }
        }

        private void StartReplay()
        {
            Restart();
            replaying = true;
            quality = 0.86f;
            tiltDegrees = 18f;
        }

        private void Restart()
        {
            gate.Reset();
            replaying = false;
            replayFinished = false;
            replaySeconds = 0f;
        }

        private void OnGUI()
        {
            if (title == null)
                InitializeStyles();

            Matrix4x4 original = GUI.matrix;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1100f, Screen.height / 700f), 1f, 1.6f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;
            float screenHeight = Screen.height / scale;
            Fill(new Rect(0f, 0f, screenWidth, screenHeight), Background);

            bool stacked = screenWidth < 740f;
            const float captionTop = 62f;
            const float captionGap = 6f;
            float horizontalPadding = stacked ? 24f : 64f;
            float defaultControlsTop = stacked ? 118f : 114f;
            float pageHeight = stacked ? 962f : 610f;
            float width = Mathf.Min(1120f, screenWidth - horizontalPadding);
            float captionHeight = body.CalcHeight(Caption, width);
            float controlsTop = Mathf.Max(defaultControlsTop, captionTop + captionHeight + captionGap);
            float scrollWidth = 0f;
            if (screenHeight < pageHeight + (controlsTop - defaultControlsTop))
            {
                scrollWidth = 16f;
                width = Mathf.Min(1120f, screenWidth - horizontalPadding - scrollWidth);
                captionHeight = body.CalcHeight(Caption, width);
                controlsTop = Mathf.Max(defaultControlsTop, captionTop + captionHeight + captionGap);
            }

            pageHeight += controlsTop - defaultControlsTop;
            scroll = GUI.BeginScrollView(new Rect(0f, 0f, screenWidth, screenHeight), scroll,
                new Rect(0f, 0f, screenWidth - scrollWidth, pageHeight));
            float left = (screenWidth - scrollWidth - width) * 0.5f;
            GUI.Label(new Rect(left, 14f, width, 45f), "AR placement", title);
            GUI.Label(new Rect(left, captionTop, width, captionHeight), Caption, body);

            const float controlsHeight = 476f;
            if (stacked)
            {
                DrawControls(new Rect(left, controlsTop, width, controlsHeight));
                DrawPreview(new Rect(left, controlsTop + controlsHeight + 12f, width, 326f));
            }
            else
            {
                const float controlWidth = 330f;
                DrawControls(new Rect(left, controlsTop, controlWidth, controlsHeight));
                DrawPreview(new Rect(left + controlWidth + 14f, controlsTop, width - controlWidth - 14f, controlsHeight));
            }

            GUI.EndScrollView();
            GUI.matrix = original;
        }

        private void DrawControls(Rect box)
        {
            GUI.BeginGroup(box);
            Fill(new Rect(0f, 0f, box.width, box.height), Panel);
            const float inset = 15f;
            float width = box.width - 2f * inset;
            GUI.Label(new Rect(inset, 9f, width, 29f), "Tracking inputs", heading);

            bool enabled = GUI.enabled;
            GUI.enabled = enabled && !gate.IsPlaced && !replaying;
            quality = Slider(inset, 43f, width, "Frame quality", $"{quality:P0}", quality, 0f, 1f);
            tiltDegrees = Slider(inset, 98f, width, "Device tilt", $"{tiltDegrees:F0} deg", tiltDegrees, -60f, 60f);
            GUI.enabled = enabled;

            GUI.Label(new Rect(inset, 151f, width, 36f), TrackingHint(), hint);
            Fill(new Rect(inset, 194f, width, 7f), Grid);
            Fill(new Rect(inset, 194f, width * gate.Progress, 7f), gate.IsPlaced ? Amber : Mint);

            GUI.enabled = enabled && gate.IsReady && !replaying;
            if (GUI.Button(new Rect(inset, 211f, width, 38f), "Confirm placement", button))
                gate.TryPlace();
            GUI.enabled = enabled;

            float half = (width - 8f) * 0.5f;
            if (GUI.Button(new Rect(inset, 257f, half, 36f), "Run replay", button))
                StartReplay();
            if (GUI.Button(new Rect(inset + half + 8f, 257f, half, 36f), "Reset", button))
                Restart();

            Fill(new Rect(inset, 312f, width, 1f), Grid);
            GUI.Label(new Rect(inset, 321f, width, 27f), "Anchor offsets", heading);
            separation = Slider(inset, 354f, width, "Separation", $"{separation:F1} m", separation, 1f, 3.5f);
            objectScale = Slider(inset, 411f, width, "Object scale", $"{objectScale:F2}x", objectScale, 0.5f, 1.5f);
            GUI.EndGroup();
        }

        private string TrackingHint()
        {
            if (gate.IsPlaced)
                return "Anchor confirmed. Change separation or scale.";
            if (quality < PlacementGate.MinimumQuality)
                return "Raise image quality to at least 70%.";
            if (Mathf.Abs(tiltDegrees) > PlacementGate.MaximumTilt)
                return "Tilt outside +/-30 degrees. Progress reset.";
            if (gate.IsReady)
                return replayFinished ? "Replay finished. Confirm the anchor." : "Signal stable. Confirm the anchor.";
            return $"Hold usable input for {PlacementGate.RequiredSeconds:F1} seconds.";
        }

        private float Slider(float x, float y, float width, string name, string value,
            float current, float minimum, float maximum)
        {
            GUI.Label(new Rect(x, y, width - 85f, 23f), name, body);
            GUI.Label(new Rect(x + width - 76f, y, 76f, 23f), value, hint);
            return GUI.HorizontalSlider(new Rect(x, y + 26f, width, 25f), current, minimum, maximum);
        }

        private void DrawPreview(Rect box)
        {
            GUI.BeginGroup(box);
            Fill(new Rect(0f, 0f, box.width, box.height), Panel);
            GUI.Label(new Rect(15f, 9f, box.width - 30f, 29f), "Placement preview", heading);
            string label = gate.IsPlaced ? "Anchor locked" : "Preview before confirmation";
            GUI.Label(new Rect(15f, 43f, box.width - 30f, 23f), label, hint);
            if (Event.current.type == EventType.Repaint)
            {
                Rect field = new Rect(14f, 76f, box.width - 28f, box.height - 123f);
                Fill(field, Background);
                float meter = Mathf.Min(field.width / 5f, 107f);
                float center = field.center.x;
                float ground = field.y + field.height * 0.65f;
                for (int index = -2; index <= 2; index++)
                    Fill(new Rect(center + index * meter - 0.5f, field.y + 7f, 1f, field.height - 14f), Grid);
                Fill(new Rect(field.x + 7f, ground - 0.5f, field.width - 14f, 1f), Grid);

                var offsets = AnchorLayout.Centers(separation);
                float left = center + offsets.Left * meter;
                float right = center + offsets.Right * meter;
                float size = Mathf.Min(meter * 0.57f, 63f) * objectScale;
                DrawMarker(left, ground, Mint);
                DrawMarker(right, ground, Amber);
                DrawMarker(center, ground, Ink);
                DrawPiece(left, ground, size, Mint);
                DrawPiece(right, ground, size, Amber);

                float dimension = ground + 23f;
                Fill(new Rect(left, dimension, right - left, 1f), Muted);
                Fill(new Rect(left, dimension - 4f, 1f, 9f), Muted);
                Fill(new Rect(right, dimension - 4f, 1f, 9f), Muted);
            }
            GUI.Label(new Rect(15f, box.height - 35f, box.width - 30f, 25f),
                "White marker: anchor origin", hint);
            GUI.EndGroup();
        }

        private static void DrawPiece(float center, float bottom, float size, Color color)
        {
            Fill(new Rect(center - size * 0.42f, bottom - size * 0.62f, size * 0.84f, size * 0.54f), color);
            Fill(new Rect(center - size * 0.29f, bottom - size, size * 0.58f, size * 0.33f), color);
            Fill(new Rect(center - size * 0.19f, bottom - size * 0.89f, size * 0.38f, size * 0.09f), Background);
        }

        private static void DrawMarker(float x, float y, Color color)
        {
            Fill(new Rect(x - 6f, y - 1f, 12f, 2f), color);
            Fill(new Rect(x - 1f, y - 6f, 2f, 12f), color);
        }

        private static void Fill(Rect area, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void InitializeStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 37, fontStyle = FontStyle.Bold };
            title.normal.textColor = Ink;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
            heading.normal.textColor = Ink;
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            body.normal.textColor = Ink;
            hint = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            hint.normal.textColor = Muted;
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        }
    }
}
