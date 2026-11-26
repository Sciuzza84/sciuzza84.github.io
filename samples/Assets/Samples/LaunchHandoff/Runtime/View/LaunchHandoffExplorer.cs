using System.Collections.Generic;
using UnityEngine;
using PlanarVector = System.Numerics.Vector2;

namespace GameplaySamples.LaunchHandoff
{
    public sealed class LaunchHandoffExplorer : MonoBehaviour
    {
        private const float FlightDuration = 0.60f;
        private const float SampleInterval = 0.04f;
        private static readonly LaunchGuide Guide = new LaunchGuide(
            new PlanarVector(0f, 0.7f), new PlanarVector(3.6f, 1.8f), 0.9f);
        private static readonly Color Ink = new Color32(235, 243, 235, 255);
        private static readonly Color GuideColor = new Color32(111, 148, 160, 255);
        private static readonly Color ChartColor = new Color32(27, 46, 54, 255);
        private static readonly Color ZeroColor = new Color32(244, 187, 113, 255);
        private static readonly Color PreserveColor = new Color32(119, 222, 183, 255);

        private enum Phase { Ready, Guided, Flight, Complete }

        private Lane[] lanes;
        private Texture2D dot;
        private Phase phase;
        private float guidedElapsed;
        private float flightElapsed;
        private float nextSample;
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle text;
        private GUIStyle button;

        private void Awake()
        {
            dot = CreateDot();
            lanes = new[] { CreateLane("Zero velocity", false, ZeroColor, 0f),
                CreateLane("Preserve velocity", true, PreserveColor, 2f) };
            ResetBodies();
        }

        private void OnDestroy()
        {
            Destroy(dot);
        }

        private Lane CreateLane(string label, bool preserve, Color color, float depth)
        {
            var ball = new GameObject(label);
            ball.transform.SetParent(transform, false);
            Rigidbody body = ball.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.linearDamping = 0f;
            return new Lane(label, preserve, color, depth, body);
        }

        private void ResetBodies()
        {
            guidedElapsed = 0f;
            flightElapsed = 0f;
            nextSample = SampleInterval;
            foreach (Lane lane in lanes)
            {
                lane.Body.isKinematic = true;
                lane.Body.position = BodyPosition(Guide.Start, lane.Depth);
                lane.Trace.Clear();
            }
        }

        private void RunComparison()
        {
            ResetBodies();
            phase = Phase.Guided;
        }

        private void FixedUpdate()
        {
            if (phase == Phase.Guided)
                AdvanceGuide();
            else if (phase == Phase.Flight)
                ObserveFlight();
        }

        private void AdvanceGuide()
        {
            guidedElapsed += Time.fixedDeltaTime;
            if (guidedElapsed < Guide.Duration - 0.00001f)
            {
                PlanarVector point = Guide.PositionAt(guidedElapsed);
                foreach (Lane lane in lanes)
                    lane.Body.MovePosition(BodyPosition(point, lane.Depth));
                return;
            }

            foreach (Lane lane in lanes)
            {
                lane.Body.position = BodyPosition(Guide.End, lane.Depth);
                lane.Body.isKinematic = false;
                lane.Body.linearVelocity = BodyPosition(Guide.ReleaseVelocity(lane.Preserve), 0f);
                lane.Body.WakeUp();
                lane.Capture(0f);
            }
            phase = Phase.Flight;
        }

        private void ObserveFlight()
        {
            // FixedUpdate observes the physics step that just completed.
            flightElapsed += Time.fixedDeltaTime;
            bool finished = flightElapsed >= FlightDuration - 0.00001f;
            if (!finished && flightElapsed < nextSample)
                return;

            foreach (Lane lane in lanes)
            {
                lane.Capture(flightElapsed);
                if (finished)
                    lane.Body.isKinematic = true;
            }
            nextSample = flightElapsed + SampleInterval;
            if (finished)
                phase = Phase.Complete;
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            float panelWidth = Mathf.Min(900f, Screen.width - 32f);
            GUILayout.BeginArea(new Rect((Screen.width - panelWidth) * 0.5f, 14f, panelWidth, Screen.height - 28f));
            scroll = GUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            float contentWidth = panelWidth - GUI.skin.verticalScrollbar.fixedWidth - 8f;
            GUILayout.BeginVertical(GUILayout.Width(contentWidth));
            GUILayout.Label("Launch handoff", title);
            GUILayout.Label("Both balls follow the same guide. At release, one keeps the guide velocity and the other starts at zero. Gravity acts on both.", text);
            GUILayout.Label($"Incoming velocity {Format(Guide.Velocity.X, Guide.Velocity.Y)} m/s", heading);

            if (GUILayout.Button("Run comparison", button, GUILayout.Height(38f)))
                RunComparison();

            bool stacked = contentWidth < 640f;
            if (!stacked)
                GUILayout.BeginHorizontal();
            foreach (Lane lane in lanes)
                DrawLane(lane, stacked ? contentWidth : (contentWidth - 8f) * 0.5f);
            if (!stacked)
                GUILayout.EndHorizontal();

            GUILayout.Label("Values are horizontal, vertical. Unity Rigidbody positions and velocities are sampled during flight.", text);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawLane(Lane lane, float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            GUILayout.Space(10f);
            heading.normal.textColor = lane.Color;
            GUILayout.Label(lane.Label, heading);
            heading.normal.textColor = Ink;

            Rect chart = GUILayoutUtility.GetRect(width, 122f);
            if (Event.current.type == EventType.Repaint)
                DrawTrace(lane, chart);

            if (lane.Trace.Count == 0)
            {
                GUILayout.Label(phase == Phase.Guided ? "Moving on guide…" : "Ready on guide.", text);
            }
            else
            {
                FlightSample release = lane.Trace[0];
                FlightSample latest = lane.Trace[lane.Trace.Count - 1];
                GUILayout.Label($"At release  {Format(release.Velocity.x, release.Velocity.y)} m/s", text);
                GUILayout.Label($"After {latest.Time:0.00} s  {Format(latest.Velocity.x, latest.Velocity.y)} m/s", text);
                GUILayout.Label($"Horizontal travel  {latest.Position.x - release.Position.x:0.00} m", text);
            }
            GUILayout.EndVertical();
        }

        private void DrawTrace(Lane lane, Rect chart)
        {
            DrawTinted(chart, Texture2D.whiteTexture, ChartColor);
            Vector2 guideStart = Project(Guide.Start.X, Guide.Start.Y, chart);
            Vector2 guideEnd = Project(Guide.End.X, Guide.End.Y, chart);
            DrawLine(guideStart, guideEnd, GuideColor);
            DrawLine(new Vector2(guideEnd.x, chart.y + 8f), new Vector2(guideEnd.x, chart.yMax - 8f), GuideColor);
            Color trail = lane.Color;
            trail.a = 0.58f;
            foreach (FlightSample sample in lane.Trace)
                DrawDot(Project(sample.Position.x, sample.Position.y, chart), 4f, trail);
            Vector3 position = lane.Body.position;
            DrawDot(Project(position.x, position.y, chart), 12f, lane.Color);
        }

        private static Vector2 Project(float x, float y, Rect chart)
        {
            return new Vector2(chart.x + 12f + x / 6.4f * (chart.width - 24f),
                chart.yMax - 10f - (y + 0.45f) / 3.1f * (chart.height - 20f));
        }

        private void DrawDot(Vector2 center, float size, Color color)
        {
            float radius = size * 0.5f;
            DrawTinted(new Rect(center.x - radius, center.y - radius, size, size), dot, color);
        }

        private static void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            Vector2 delta = end - start;
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            DrawTinted(new Rect(start.x, start.y, delta.magnitude, 1.5f), Texture2D.whiteTexture, color);
            GUI.matrix = previous;
        }

        private static void DrawTinted(Rect rect, Texture texture, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = previous;
        }

        private static Texture2D CreateDot()
        {
            const int size = 16;
            const float center = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                pixels[y * size + x] = new Color32(255, 255, 255, dx * dx + dy * dy <= center * center ? (byte)255 : (byte)0);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = true };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            title.normal.textColor = Ink;
            heading.normal.textColor = Ink;
            text.normal.textColor = Ink;
        }

        private static Vector3 BodyPosition(PlanarVector point, float depth) => new Vector3(point.X, point.Y, depth);

        private static string Format(float x, float y) => $"({x:0.00}, {y:0.00})";

        private sealed class Lane
        {
            public readonly string Label;
            public readonly bool Preserve;
            public readonly Color Color;
            public readonly float Depth;
            public readonly Rigidbody Body;
            public readonly List<FlightSample> Trace = new List<FlightSample>(18);

            public Lane(string label, bool preserve, Color color, float depth, Rigidbody body)
            {
                Label = label;
                Preserve = preserve;
                Color = color;
                Depth = depth;
                Body = body;
            }

            public void Capture(float time) => Trace.Add(new FlightSample(time, Body.position, Body.linearVelocity));
        }

        private readonly struct FlightSample
        {
            public readonly float Time;
            public readonly Vector3 Position;
            public readonly Vector3 Velocity;

            public FlightSample(float time, Vector3 position, Vector3 velocity)
            {
                Time = time;
                Position = position;
                Velocity = velocity;
            }
        }
    }
}
