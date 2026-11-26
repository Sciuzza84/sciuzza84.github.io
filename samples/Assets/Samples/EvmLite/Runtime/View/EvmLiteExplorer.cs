using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameplaySamples.EvmLite
{
    public sealed class EvmLiteExplorer : MonoBehaviour
    {
        private const string RoomA = "EventRoomA";
        private const string RoomB = "EventRoomB";
        private const float DefaultTraceRowHeight = 34f;
        private static readonly GUIContent Heading = new GUIContent("EVM Lite");
        private static readonly GUIContent SceneDescription = new GUIContent(
            "Switch rooms and publish one event. Watch which subscriptions survive the scene change.");
        private static readonly GUIContent NestedDescription = new GUIContent(
            "A, B and D start subscribed. A changes the list during the outer callback, then publishes again.");
        private readonly EventBus bus = new EventBus();
        private readonly int[] totals = new int[3];
        private readonly int[] lastDeliveries = new int[3];
        private readonly int[] lastReceived = new int[3];
        private EventScope persistent;
        private Scene loadedScene;
        private int activeRoom = -1;
        private int published;
        private int sceneSwitches;
        private bool switching;
        private bool showNestedDelivery;
        private IReadOnlyList<DeliveryStep> nestedSteps;
        private string status = "Loading room A…";
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle text;
        private GUIStyle hint;
        private GUIStyle number;
        private GUIStyle button;
        private GUIStyle outerPhase;
        private GUIStyle innerPhase;
        private static readonly Color Background = new Color32(13, 25, 30, 255);
        private static readonly Color Panel = new Color32(24, 42, 48, 255);
        private static readonly Color Ink = new Color32(237, 239, 229, 255);
        private static readonly Color Muted = new Color32(173, 191, 190, 255);
        private static readonly Color Accent = new Color32(121, 222, 184, 255);
        private static readonly Color Amber = new Color32(242, 186, 118, 255);

        private void Start()
        {
            persistent = bus.CreateScope();
            persistent.Subscribe<RoomSignal>(signal => Receive(2, signal));
            StartCoroutine(SwitchRoom(0));
        }

        private void OnDestroy()
        {
            persistent?.Dispose();
        }

        private IEnumerator SwitchRoom(int destination)
        {
            string target = destination == 0 ? RoomA : RoomB;
            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                status = $"Add {target} to the build scene list.";
                yield break;
            }

            switching = true;
            activeRoom = -1;
            char label = destination == 0 ? 'A' : 'B';
            status = $"Loading room {label}…";
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedScene);

            yield return SceneManager.LoadSceneAsync(target, LoadSceneMode.Additive);
            loadedScene = SceneManager.GetSceneByName(target);
            foreach (GameObject roomObject in loadedScene.GetRootGameObjects())
            {
                if (!roomObject.TryGetComponent(out RoomSignalReceiver receiver))
                    continue;

                receiver.Initialize(bus, signal => Receive(destination, signal));
                activeRoom = destination;
                sceneSwitches++;
                break;
            }

            status = activeRoom == destination
                ? $"Room {label} is active. Publish a signal to check both receivers."
                : $"Room {label} has no RoomSignalReceiver on a root object.";
            switching = false;
        }

        private void Receive(int index, RoomSignal signal)
        {
            totals[index]++;
            lastReceived[index] = signal.Sequence;
        }

        private void Publish()
        {
            int[] previous = (int[])totals.Clone();
            bus.Publish(new RoomSignal(++published));
            for (int index = 0; index < lastDeliveries.Length; index++)
                lastDeliveries[index] = totals[index] - previous[index];

            int unloadedRoom = 1 - activeRoom;
            bool passed = lastDeliveries[activeRoom] == 1 &&
                          lastDeliveries[unloadedRoom] == 0 && lastDeliveries[2] == 1;
            status = passed
                ? $"Signal {published}: active room +1, unloaded room +0, persistent +1."
                : $"Signal {published}: unexpected delivery. Check the counts below.";
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            Matrix4x4 original = GUI.matrix;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1020f, Screen.height / 690f), 1f, 1.65f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;
            float screenHeight = Screen.height / scale;
            Fill(new Rect(0f, 0f, screenWidth, screenHeight), Background);

            bool narrow = screenWidth < 660f;
            float margin = narrow ? 24f : 64f;
            float innerWidth = screenWidth;
            float width = Mathf.Min(1140f, innerWidth - margin);
            float left = (innerWidth - width) * 0.5f;
            var layout = MeasurePage(left, width, narrow);
            if (screenHeight < layout.PageHeight)
            {
                innerWidth -= 16f;
                width = Mathf.Min(1140f, innerWidth - margin);
                left = (innerWidth - width) * 0.5f;
                layout = MeasurePage(left, width, narrow);
            }

            scroll = GUI.BeginScrollView(new Rect(0f, 0f, screenWidth, screenHeight), scroll,
                new Rect(0f, 0f, innerWidth, layout.PageHeight), false, false);
            GUI.Label(layout.Heading, Heading, title);
            bool changeMode = DrawButton(layout.Mode,
                showNestedDelivery ? "Scene lifetime" : "Nested delivery", true);
            GUI.Label(layout.Description, showNestedDelivery ? NestedDescription : SceneDescription, text);
            float actionTop = layout.Description.yMax + 6f;
            if (showNestedDelivery)
                DrawNestedDelivery(left, width, actionTop, layout.TraceRowHeight);
            else
                DrawSceneLifetime(left, width, narrow, actionTop);
            GUI.EndScrollView();
            GUI.matrix = original;

            if (changeMode)
            {
                showNestedDelivery = !showNestedDelivery;
                scroll = Vector2.zero;
            }
        }

        private (Rect Heading, Rect Mode, Rect Description, float TraceRowHeight, float PageHeight)
            MeasurePage(float left, float width, bool narrow)
        {
            const float gap = 12f;
            float modeWidth = Mathf.Min(155f, width);
            bool stacked = title.CalcSize(Heading).x + gap + modeWidth > width;
            var heading = new Rect(left, 20f, stacked ? width : width - modeWidth - gap, 48f);
            var mode = stacked
                ? new Rect(left, 76f, modeWidth, 40f)
                : new Rect(left + width - modeWidth, 24f, modeWidth, 38f);
            GUIContent description = showNestedDelivery ? NestedDescription : SceneDescription;
            float descriptionHeight = Mathf.Max(58f, Mathf.Ceil(text.CalcHeight(description, width)));
            var subtitle = new Rect(left, stacked ? 123f : 71f, width, descriptionHeight);
            float rowHeight = DefaultTraceRowHeight;
            float pageHeight = showNestedDelivery ? 561f : narrow ? 720f : 494f;
            if (showNestedDelivery && nestedSteps != null)
            {
                var action = new GUIContent();
                foreach (DeliveryStep step in nestedSteps)
                {
                    action.text = step.Action;
                    rowHeight = Mathf.Max(rowHeight, Mathf.Ceil(text.CalcHeight(action, width - 109f)));
                }

                pageHeight += nestedSteps.Count * (rowHeight - DefaultTraceRowHeight);
            }

            return (heading, mode, subtitle, rowHeight, pageHeight + subtitle.yMax - 129f);
        }

        private void DrawSceneLifetime(float left, float width, bool narrow, float actionTop)
        {
            const float gap = 8f;
            float smallButton = Mathf.Min(148f, (width - gap) / 2f);
            if (DrawButton(new Rect(left, actionTop, smallButton, 40f), "Load room A", activeRoom != 0))
                StartCoroutine(SwitchRoom(0));
            if (DrawButton(new Rect(left + smallButton + gap, actionTop, smallButton, 40f), "Load room B", activeRoom != 1))
                StartCoroutine(SwitchRoom(1));

            float publishLeft = narrow ? left : left + (smallButton + gap) * 2f;
            float publishTop = narrow ? actionTop + 48f : actionTop;
            if (DrawButton(new Rect(publishLeft, publishTop, 162f, 40f), "Publish signal", activeRoom >= 0))
                Publish();

            float statusTop = actionTop + (narrow ? 97f : 55f);
            Fill(new Rect(left, statusTop, width, 66f), Panel);
            GUI.Label(new Rect(left + 13f, statusTop + 7f, width - 26f, 51f), status, text);
            float cardTop = statusTop + 83f;
            float cardWidth = narrow ? width : (width - gap * 2f) / 3f;
            for (int index = 0; index < 3; index++)
            {
                float x = left + (narrow ? 0f : index * (cardWidth + gap));
                float y = cardTop + (narrow ? index * 108f : 0f);
                DrawReceiver(new Rect(x, y, cardWidth, 100f), index);
            }

            float summaryTop = cardTop + (narrow ? 329f : 108f);
            GUI.Label(new Rect(left, summaryTop, width, 36f),
                $"{published} signals sent · {sceneSwitches} room loads", hint);
        }

        private void DrawNestedDelivery(float left, float width, float actionTop, float rowHeight)
        {
            if (DrawButton(new Rect(left, actionTop, 162f, 40f), "Run nested trace", true))
                nestedSteps = NestedDeliveryExercise.Run();

            if (nestedSteps == null)
                return;

            float panelTop = actionTop + 55f;
            float panelHeight = 54f + nestedSteps.Count * rowHeight;
            Fill(new Rect(left, panelTop, width, panelHeight), Panel);
            GUI.Label(new Rect(left + 12f, panelTop + 8f, width - 24f, 28f), "Delivery order", number);
            for (int index = 0; index < nestedSteps.Count; index++)
            {
                DeliveryStep step = nestedSteps[index];
                bool inner = step.Phase == DeliveryPhase.Inner;
                float y = panelTop + 41f + index * rowHeight;
                Fill(new Rect(left + 12f, y + 4f, 2f, 24f), inner ? Accent : Amber);
                GUI.Label(new Rect(left + 20f, y, 24f, rowHeight), (index + 1).ToString(), hint);
                GUI.Label(new Rect(left + 45f, y, 48f, rowHeight), inner ? "Inner" : "Outer", inner ? innerPhase : outerPhase);
                GUI.Label(new Rect(left + 97f, y, width - 109f, rowHeight), step.Action, text);
            }

            GUI.Label(new Rect(left, panelTop + panelHeight + 8f, width, 55f),
                "B is removed before its turn. C joins the inner publish. D receives both, inner first.", hint);
        }

        private bool DrawButton(Rect bounds, string label, bool available)
        {
            bool original = GUI.enabled;
            GUI.enabled = original && !switching && available;
            bool clicked = GUI.Button(bounds, label, button);
            GUI.enabled = original;
            return clicked;
        }

        private void DrawReceiver(Rect bounds, int index)
        {
            string label = index == 2 ? "Persistent receiver" : index == 0 ? "Room A" : "Room B";
            string state = index == 2 ? "Across scene changes" : index == activeRoom ? "Loaded" : "Unloaded";
            if (lastReceived[index] > 0)
                state += $" · last signal #{lastReceived[index]}";
            Fill(bounds, Panel);
            Fill(new Rect(bounds.x, bounds.y, bounds.width, 2f), index == activeRoom || index == 2 ? Accent : Amber);
            GUI.Label(new Rect(bounds.x + 12f, bounds.y + 12f, bounds.width - 24f, 23f), label, text);
            GUI.Label(new Rect(bounds.x + 12f, bounds.y + 35f, bounds.width - 24f, 20f), state, hint);
            string receipts = published == 0 ? $"{totals[index]} total" : $"{totals[index]} total  /  +{lastDeliveries[index]} last";
            GUI.Label(new Rect(bounds.x + 12f, bounds.y + 61f, bounds.width - 24f, 30f), receipts, number);
        }

        private void CreateStyles()
        {
            title = Label(36, Ink, FontStyle.Bold);
            text = Label(17, Ink);
            hint = Label(14, Muted);
            number = Label(19, Accent, FontStyle.Bold);
            outerPhase = Label(13, Amber, FontStyle.Bold);
            innerPhase = Label(13, Accent, FontStyle.Bold);
            button = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }

        private static GUIStyle Label(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = weight,
                wordWrap = true
            };
            style.normal.textColor = color;
            return style;
        }

        private static void Fill(Rect rectangle, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color original = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
            GUI.color = original;
        }
    }
}
