using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameplaySamples.Dungeon
{
    public sealed class DungeonExplorer : MonoBehaviour
    {
        private const string SeedField = "DungeonSeed";
        private const float PresetPanelHeight = 402f;
        private static readonly Color Paper = Rgb(0xECECE2);
        private static readonly Color Muted = Rgb(0xAFBFBC);
        private static readonly Color Ink = Rgb(0x111C21);
        private static readonly Color Panel = Rgb(0x1B2A2F);
        private static readonly Color Border = Rgb(0x35484B);
        private static readonly Color MainRoute = Rgb(0x7BDFBB);
        private static readonly Color SideRoute = Rgb(0xF2B973);
        private static readonly Color Rejection = Rgb(0xFF8F8F);

        private readonly System.Random seedPicker = new System.Random();
        private readonly List<Texture2D> textures = new List<Texture2D>();
        private DungeonLayout layout;
        private DungeonBranchPreset activePreset;
        private DungeonPresetValidation presetValidation;
        private string[] roomText, weightText;
        private bool showPresets;
        private int branches = 4;
        private string seedText;
        private string seedError;
        private string mapCaption;
        private string branchSummary;
        private string rejectionSummary;
        private Vector2 scrollPosition;
        private Vector2 controlsScrollPosition;
        private GUIStyle title, heading, body, caption, alignedCaption, value, marker;
        private GUIStyle primaryButton, secondaryButton, seedInput, compactButton, fieldError, totalError;

        private void Awake()
        {
            LoadExample(invalid: false);
            SetSeed(19);
        }

        private void SetSeed(int seed)
        {
            seedText = seed.ToString(CultureInfo.InvariantCulture);
            seedError = string.Empty;
            layout = DungeonGenerator.Generate(seed, branches, activePreset);
            mapCaption = "Seed " + seedText;
            branchSummary = $"Placed {layout.PlacedBranches} of {layout.RequestedBranches} branches. Rejected candidates: {layout.RejectedCandidates}.";
            rejectionSummary = layout.FirstRejection?.Reason switch
            {
                DungeonRejectionReason.Occupied => "First rejection: the marked cell already had a room.",
                DungeonRejectionReason.InsufficientClearance => "First rejection: the marked cell touched another route.",
                DungeonRejectionReason.OutsideGrid => "First rejection: the branch crossed the marked grid edge.",
                _ => string.Empty
            };
        }

        private void ApplyInput()
        {
            if (TryReadSeed(out int seed))
                SetSeed(seed);
        }

        private bool TryReadSeed(out int seed)
        {
            if (int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
                return true;

            seedError = "Enter a whole number from -2147483648 to 2147483647.";
            return false;
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            // Scale the entire surface, including hit targets and text, on high-density canvases.
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 920f, Screen.height / 570f), 1f, 2f);
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            bool stacked = width < 740f;
            float inset = stacked ? 12f : 24f;
            float top = stacked ? 68f : 84f;
            float mapHeight = stacked ? Mathf.Clamp(width * 0.68f, 224f, 388f) : Mathf.Max(352f, height - top - inset);
            float weightsButtonY = string.IsNullOrEmpty(rejectionSummary) ? 274f : 319f;
            float controlsHeight = weightsButtonY + 45f + (showPresets ? PresetPanelHeight : 0f);
            float pageHeight = top + inset + (stacked ? mapHeight + 12f + controlsHeight : mapHeight);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            Fill(new Rect(0, 0, width, height), Ink);

            bool canScroll = pageHeight > height + 1f;
            if (canScroll)
            {
                width -= 18f;
                scrollPosition = GUI.BeginScrollView(new Rect(0, 0, width + 18f, height), scrollPosition,
                    new Rect(0, 0, width, pageHeight), false, false);
            }

            float contentWidth = Mathf.Min(1340f, width - 2f * inset);
            float left = (width - contentWidth) * 0.5f;
            GUI.Label(new Rect(left, stacked ? 6f : 12f, contentWidth, 40f), "Dungeon generator", title);
            GUI.Label(new Rect(left, stacked ? 42f : 52f, contentWidth, 22f), "One main route, with room for side paths.", body);

            if (stacked)
            {
                DrawMap(new Rect(left, top, contentWidth, mapHeight));
                DrawControls(new Rect(left, top + mapHeight + 12f, contentWidth, controlsHeight), weightsButtonY);
            }
            else
            {
                float rail = Mathf.Clamp(contentWidth * 0.27f, 224f, 290f);
                if (controlsHeight > mapHeight)
                {
                    float railContentWidth = rail - 18f;
                    controlsScrollPosition = GUI.BeginScrollView(new Rect(left, top, rail, mapHeight), controlsScrollPosition,
                        new Rect(0f, 0f, railContentWidth, controlsHeight), false, true);
                    DrawControls(new Rect(0f, 0f, railContentWidth, controlsHeight), weightsButtonY);
                    GUI.EndScrollView();
                }
                else
                {
                    controlsScrollPosition = Vector2.zero;
                    DrawControls(new Rect(left, top, rail, controlsHeight), weightsButtonY);
                }
                DrawMap(new Rect(left + rail + 14f, top, contentWidth - rail - 14f, mapHeight));
            }

            if (canScroll)
                GUI.EndScrollView();
            GUI.matrix = previousMatrix;
        }

        private void DrawControls(Rect box, float weightsButtonY)
        {
            GUI.BeginGroup(box);
            Fill(new Rect(0, 0, box.width, box.height), Panel);
            const float x = 16f;
            float w = box.width - 2f * x;
            GUI.Label(new Rect(x, 10, w, 24), "Generation", heading);
            GUI.Label(new Rect(x, 42, w, 20), "Seed", body);
            if (GUI.GetNameOfFocusedControl() == SeedField && Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                ApplyInput();
                Event.current.Use();
            }
            GUI.SetNextControlName(SeedField);
            seedText = GUI.TextField(new Rect(x, 64, w, 35), seedText, 24, seedInput);

            GUI.Label(new Rect(x, 107, w, 22), "Requested branches", body);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && branches > 0;
            if (GUI.Button(new Rect(x, 132, 42, 36), "-", secondaryButton))
                ChangeBranches(-1);
            GUI.enabled = wasEnabled;
            GUI.Label(new Rect(x + 42f, 132, w - 84, 36), branches.ToString(), value);
            GUI.enabled = wasEnabled && branches < DungeonGenerator.MaxBranches;
            if (GUI.Button(new Rect(x + w - 42f, 132, 42, 36), "+", secondaryButton))
                ChangeBranches(1);
            GUI.enabled = wasEnabled;

            float half = (w - 6f) * 0.5f;
            if (GUI.Button(new Rect(x, 181, half, 38), "Generate", primaryButton))
                ApplyInput();
            if (GUI.Button(new Rect(x + half + 6f, 181, half, 38), "New seed", secondaryButton))
                SetSeed(seedPicker.Next());
            string feedback = string.IsNullOrEmpty(seedError) ? branchSummary : seedError;
            GUI.Label(new Rect(x, 225f, w, 44), feedback, caption);
            GUI.Label(new Rect(x, 266f, w, 44), rejectionSummary, caption);
            if (GUI.Button(new Rect(x, weightsButtonY, w, 34f), showPresets ? "Hide length weights" : "Edit length weights", secondaryButton))
                showPresets = !showPresets;
            if (showPresets)
                DrawPresetControls(x, weightsButtonY + 45f, w);
            GUI.EndGroup();
        }

        private void LoadExample(bool invalid)
        {
            (roomText, weightText) = DungeonBranchPreset.Example(invalid);
            presetValidation = DungeonBranchPreset.Validate(roomText, weightText);
        }

        private void DrawPresetControls(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 24f), "Straight rooms before a bend", caption);
            float buttonWidth = (width - 6f) / 2;
            if (GUI.Button(new Rect(x, y + 30f, buttonWidth, 32f), "Valid preset", compactButton))
                LoadExample(invalid: false);
            if (GUI.Button(new Rect(x + buttonWidth + 6f, y + 30f, buttonWidth, 32f), "Invalid preset", compactButton))
                LoadExample(invalid: true);

            const float rowNumberWidth = 19f;
            float fieldWidth = (width - rowNumberWidth - 8f) / 2;
            float roomX = x + rowNumberWidth;
            float weightX = roomX + fieldWidth + 8f;
            GUI.Label(new Rect(roomX, y + 67f, fieldWidth, 20f), "Rooms", caption);
            GUI.Label(new Rect(weightX, y + 67f, fieldWidth, 20f), "Weight (%)", caption);
            bool edited = false;
            for (int index = 0; index < roomText.Length; index++)
            {
                float rowY = y + 89f + index * 64f;
                GUI.Label(new Rect(x, rowY, rowNumberWidth, 28f), (index + 1).ToString(CultureInfo.InvariantCulture), caption);
                edited |= DrawPresetField(new Rect(roomX, rowY, fieldWidth, 28f), ref roomText[index],
                    presetValidation.RoomErrors[index], 2);
                edited |= DrawPresetField(new Rect(weightX, rowY, fieldWidth, 28f), ref weightText[index],
                    presetValidation.WeightErrors[index], 3);
            }
            if (edited)
                presetValidation = DungeonBranchPreset.Validate(roomText, weightText);

            GUI.Label(new Rect(x, y + 280f, width, 32f), presetValidation.WeightFeedback,
                presetValidation.ValidWeightTotal ? caption : totalError);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && presetValidation.Preset != null;
            if (GUI.Button(new Rect(x, y + 315f, buttonWidth, 34f), "Apply weights", compactButton))
                ApplyPreset(presetValidation.Preset);
            GUI.enabled = wasEnabled && activePreset != null;
            if (GUI.Button(new Rect(x + buttonWidth + 6f, y + 315f, buttonWidth, 34f), "Restore default", compactButton))
                ApplyPreset(null);
            GUI.enabled = wasEnabled;

            string status = GetPresetStatus();
            GUI.Label(new Rect(x, y + 351f, width, 37f), status, caption);
        }

        private bool DrawPresetField(Rect rect, ref string field, string error, int maxLength)
        {
            if (error != null)
            {
                Fill(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), Rejection);
                GUI.Label(new Rect(rect.x, rect.yMax + 2f, rect.width, 32f), error, fieldError);
            }

            string before = field;
            field = GUI.TextField(rect, field, maxLength, seedInput);
            return field != before;
        }

        private void ApplyPreset(DungeonBranchPreset preset)
        {
            if (!TryReadSeed(out int seed))
                return;

            activePreset = preset;
            SetSeed(seed);
        }

        private string GetPresetStatus()
        {
            if (!string.IsNullOrEmpty(seedError))
                return "Enter a valid seed above to apply.";
            if (activePreset == null)
                return "The map uses its default weights.";

            return activePreset == presetValidation.Preset
                ? "These weights are applied to the map."
                : "The map uses the last applied weights.";
        }

        private void ChangeBranches(int change)
        {
            branches = Mathf.Clamp(branches + change, 0, DungeonGenerator.MaxBranches);
            ApplyInput();
        }

        private void DrawMap(Rect box)
        {
            GUI.BeginGroup(box);
            Fill(new Rect(0, 0, box.width, box.height), Panel);
            GUI.Label(new Rect(14f, 5f, box.width * 0.49f, 34f), "Route map", heading);
            GUI.Label(new Rect(box.width * 0.42f, 5f, box.width * 0.58f - 14f, 34f), mapCaption, alignedCaption);
            Rect field = new Rect(8f, 41f, box.width - 16f, box.height - 80f);
            Fill(field, Ink);
            float step = Mathf.Min(field.width / (layout.Width + 0.2f), field.height / (layout.Height + 0.2f));
            Vector2 origin = new Vector2(field.center.x - (layout.Width - 1) * step * 0.5f,
                field.center.y + (layout.Height - 1) * step * 0.5f);
            if (Event.current.type == EventType.Repaint)
            {
                for (int x = 0; x < layout.Width; x++)
                    for (int y = 0; y < layout.Height; y++)
                        Fill(new Rect(origin.x + x * step - 1f, origin.y - y * step - 1f, 2f, 2f), Border);

                foreach (DungeonEdge edge in layout.Edges)
                {
                    DungeonRoom first = layout.Rooms[edge.From];
                    DungeonRoom second = layout.Rooms[edge.To];
                    Vector2 a = Position(first, origin, step);
                    Vector2 b = Position(second, origin, step);
                    Color routeColor = first.IsMainRoute && second.IsMainRoute ? MainRoute : SideRoute;
                    float stroke = Mathf.Clamp(step * 0.12f, 2f, 5f);
                    float halfStroke = stroke * 0.5f;
                    Fill(Rect.MinMaxRect(Mathf.Min(a.x, b.x) - halfStroke, Mathf.Min(a.y, b.y) - halfStroke,
                        Mathf.Max(a.x, b.x) + halfStroke, Mathf.Max(a.y, b.y) + halfStroke), routeColor);
                }

                for (int index = 0; index < layout.Rooms.Count; index++)
                {
                    DungeonRoom room = layout.Rooms[index];
                    Vector2 center = Position(room, origin, step);
                    bool entrance = index == layout.EntranceIndex;
                    bool boss = index == layout.BossIndex;
                    if (entrance || boss)
                    {
                        float badge = Mathf.Clamp(step * 0.85f, 14f, 28f);
                        Rect tile = Square(center, badge);
                        Fill(tile, entrance ? Paper : SideRoute);
                        GUI.Label(tile, entrance ? "S" : "B", marker);
                    }
                    else
                    {
                        float size = Mathf.Clamp(step * (room.IsMainRoute ? 0.48f : 0.36f), 5f, 18f);
                        Fill(Square(center, size), room.IsMainRoute ? MainRoute : SideRoute);
                        if (room.IsMainRoute && size > 9f)
                            Fill(Square(center, size * 0.46f), Ink);
                    }
                }
                if (layout.FirstRejection is DungeonBranchRejection rejection)
                    DrawRejection(rejection, origin, step);
            }

            float yLegend = box.height - 36f;
            Fill(new Rect(14f, yLegend + 13f, 8f, 8f), MainRoute);
            GUI.Label(new Rect(27f, yLegend, 48, 32f), "Main", body);
            Fill(new Rect(77f, yLegend + 13f, 8f, 8f), SideRoute);
            GUI.Label(new Rect(90f, yLegend, 50, 32f), "Side", body);
            GUI.Label(new Rect(134f, yLegend, box.width - 147f, 32f), "S  Start     B  Boss", alignedCaption);
            GUI.EndGroup();
        }

        private static Vector2 Position(DungeonRoom room, Vector2 origin, float step)
        {
            return new Vector2(origin.x + room.X * step, origin.y - room.Y * step);
        }

        private void DrawRejection(DungeonBranchRejection rejection, Vector2 origin, float step)
        {
            // When the candidate lies outside the grid, outline the adjacent boundary cell.
            int x = Mathf.Clamp(rejection.X, 0, layout.Width - 1);
            int y = Mathf.Clamp(rejection.Y, 0, layout.Height - 1);
            Rect box = Square(new Vector2(origin.x + x * step, origin.y - y * step), step * 0.86f);
            float stroke = Mathf.Clamp(step * 0.08f, 1.5f, 3f);
            Fill(new Rect(box.xMin, box.yMin, box.width, stroke), Rejection);
            Fill(new Rect(box.xMin, box.yMax - stroke, box.width, stroke), Rejection);
            Fill(new Rect(box.xMin, box.yMin, stroke, box.height), Rejection);
            Fill(new Rect(box.xMax - stroke, box.yMin, stroke, box.height), Rejection);
        }

        private static Rect Square(Vector2 center, float size)
        {
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }

        private static Color Rgb(int hex)
        {
            return new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 255);
        }

        private static void Fill(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
        }

        private void CreateStyles()
        {
            title = TextStyle(30, Paper, true);
            heading = TextStyle(19, Paper, true);
            body = TextStyle(14, Muted);
            caption = TextStyle(13, Muted);
            caption.wordWrap = true;
            alignedCaption = TextStyle(13, Muted, false, TextAnchor.MiddleRight);
            value = TextStyle(20, Paper, true, TextAnchor.MiddleCenter);
            marker = TextStyle(12, Ink, true, TextAnchor.MiddleCenter);
            primaryButton = ControlStyle(Ink, MainRoute, Rgb(0xADF4D8), TextAnchor.MiddleCenter);
            secondaryButton = ControlStyle(Paper, Border, Rgb(0x4B6668), TextAnchor.MiddleCenter);
            compactButton = new GUIStyle(secondaryButton) { fontSize = 12 };
            fieldError = TextStyle(11, Rejection, false, TextAnchor.UpperLeft);
            fieldError.wordWrap = true;
            totalError = new GUIStyle(caption);
            totalError.normal.textColor = Rejection;
            seedInput = ControlStyle(Paper, Ink, Rgb(0x314247), TextAnchor.MiddleLeft);
            seedInput.padding = new RectOffset(9, 6, 0, 0);
        }

        private static GUIStyle TextStyle(int size, Color color, bool bold = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var style = new GUIStyle { font = GUI.skin.label.font, fontSize = size,
                fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, alignment = alignment };
            style.normal.textColor = color;
            return style;
        }

        private GUIStyle ControlStyle(Color color, Color background, Color highlight, TextAnchor alignment)
        {
            GUIStyle style = TextStyle(15, color, true, alignment);
            style.normal.background = Solid(background);
            Texture2D selected = Solid(highlight);
            style.hover.background = style.active.background = style.focused.background = selected;
            style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            return style;
        }

        private Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in textures)
                Destroy(texture);
        }
    }
}
