using System.Collections.Generic;
using UnityEngine;

namespace GameplaySamples.MatchRecovery
{
    public sealed class MatchRecoveryExplorer : MonoBehaviour
    {
        private const int TraceLimit = 6;
        private static readonly ScoreUpdate[] Sequence =
        {
            new ScoreUpdate(1, Team.A, 2),
            new ScoreUpdate(2, Team.B, 3),
            new ScoreUpdate(3, Team.A, 4),
            new ScoreUpdate(4, Team.B, 2),
            new ScoreUpdate(5, Team.A, 1)
        };

        private MatchReplica authority = new MatchReplica();
        private MatchReplica client = new MatchReplica();
        private MatchSnapshot previousSnapshot;
        private ScoreUpdate lastDelivery;
        private readonly List<string> trace = new List<string>(TraceLimit);
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle section;
        private GUIStyle body;
        private GUIStyle muted;
        private GUIStyle warning;
        private GUIStyle action;

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            float panel = Mathf.Min(780, Screen.width - 32);
            Rect bounds = new Rect((Screen.width - panel) / 2, 12, panel, Screen.height - 24);
            GUILayout.BeginArea(bounds);
            scroll = GUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            using (new GUILayout.VerticalScope(GUILayout.Width(panel - GUI.skin.verticalScrollbar.fixedWidth - 8)))
            {
                GUILayout.Label("Match recovery", title);
                GUILayout.Label("A scoreboard replica keeps its last complete state until an authoritative snapshot covers the missing revisions.", muted);
                GUILayout.Space(12);
                if (panel >= 560)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        DrawScore("Authority", authority.State);
                        DrawScore("Client replica", client.State);
                    }
                }
                else
                {
                    DrawScore("Authority", authority.State);
                    DrawScore("Client replica", client.State);
                }

                DrawStatus();
                GUILayout.Space(12);
                DrawControls(panel >= 560);
                GUILayout.Space(12);
                GUILayout.Label("Event trace", section);
                if (trace.Count == 0)
                    GUILayout.Label("Deliver revision 1, skip revision 2, then deliver revision 3.", muted);
                foreach (string entry in trace)
                    GUILayout.Label(entry, body);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawScore(string label, MatchSnapshot snapshot)
        {
            using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                GUILayout.Label(label, section);
                GUILayout.Label($"Revision {snapshot.Revision}   |   A {snapshot.ScoreA} : {snapshot.ScoreB} B", body);
            }
        }

        private void DrawStatus()
        {
            int minimum = client.MinimumSnapshotRevision;
            if (minimum > 0)
                GUILayout.Label($"Gap detected. Snapshot needs to cover revision {minimum} or later.", warning);
            else if (client.State.Revision < authority.State.Revision)
                GUILayout.Label("The client is behind. A later delivery will reveal the gap.", muted);
            else
                GUILayout.Label("Client is in sync.", muted);
        }

        private void DrawControls(bool wide)
        {
            if (wide)
            {
                using (new GUILayout.HorizontalScope())
                    DrawDelivery();
                using (new GUILayout.HorizontalScope())
                    DrawRecovery();
            }
            else
            {
                DrawDelivery();
                DrawRecovery();
            }
        }

        private void DrawDelivery()
        {
            int next = authority.State.Revision + 1;
            bool hasNext = next <= Sequence.Length;
            if (Button($"Deliver revision {next}", hasNext))
                Publish(true);
            if (Button($"Skip delivery {next}", hasNext))
                Publish(false);
        }

        private void DrawRecovery()
        {
            bool hasSnapshot = authority.State.Revision > 0;
            if (Button("Send older snapshot", hasSnapshot))
                SendSnapshot(previousSnapshot);
            if (Button("Recover from snapshot", hasSnapshot))
                SendSnapshot(authority.State);
            if (Button("Replay last delivery", lastDelivery.Revision > 0))
                Deliver(lastDelivery);
            if (Button("Reset"))
                ResetDemo();
        }

        private bool Button(string label, bool enabled = true)
        {
            bool previous = GUI.enabled;
            GUI.enabled = previous && enabled;
            bool clicked = GUILayout.Button(label, action, GUILayout.Height(36));
            GUI.enabled = previous;
            return clicked;
        }

        private void Publish(bool deliver)
        {
            ScoreUpdate update = Sequence[authority.State.Revision];
            previousSnapshot = authority.State;
            authority.Apply(update);
            if (deliver)
            {
                lastDelivery = update;
                Deliver(update);
            }
            else
            {
                Log($"Revision {update.Revision} reached the authority and was dropped before the client.");
            }
        }

        private void Deliver(ScoreUpdate update)
        {
            UpdateResult result = client.Apply(update);
            string message = result switch
            {
                UpdateResult.Applied => $"team {update.Team} +{update.Points}.",
                UpdateResult.AlreadySeen => "already seen, score unchanged.",
                UpdateResult.SnapshotRequired => "gap detected, waiting for recovery.",
                _ => "invalid update."
            };
            Log($"Delivery {update.Revision}: {message}");
        }

        private void SendSnapshot(MatchSnapshot snapshot)
        {
            string message = client.Install(snapshot) switch
            {
                SnapshotResult.Applied => "installed, client is in sync.",
                SnapshotResult.TooOld => "too old, client state preserved.",
                SnapshotResult.AlreadyCurrent => "already current.",
                _ => "rejected, client state preserved."
            };
            Log($"Snapshot {snapshot.Revision}: {message}");
        }

        private void Log(string entry)
        {
            if (trace.Count == TraceLimit)
                trace.RemoveAt(0);
            trace.Add(entry);
        }

        private void ResetDemo()
        {
            authority = new MatchReplica();
            client = new MatchReplica();
            previousSnapshot = default;
            lastDelivery = default;
            trace.Clear();
        }

        private void CreateStyles()
        {
            var ink = new Color32(235, 243, 235, 255);
            var soft = new Color32(166, 192, 187, 255);
            var amber = new Color32(244, 187, 113, 255);
            body = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, normal = { textColor = ink } };
            title = new GUIStyle(body) { fontSize = 34, fontStyle = FontStyle.Bold };
            section = new GUIStyle(body) { fontSize = 18, fontStyle = FontStyle.Bold };
            muted = new GUIStyle(body) { normal = { textColor = soft } };
            warning = new GUIStyle(body) { normal = { textColor = amber } };
            action = new GUIStyle(GUI.skin.button) { fontSize = 14, wordWrap = true };
        }
    }
}
