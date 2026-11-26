using UnityEngine;

namespace GameplaySamples.DailyChallenge
{
    public sealed class DailyChallengeExplorer : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(235, 243, 235, 255);
        private static readonly Color Mint = new Color32(119, 222, 183, 255);
        private static readonly Color Amber = new Color32(244, 187, 113, 255);
        private static readonly Color Slot = new Color32(53, 71, 78, 255);

        private readonly DailyChallengeLedger ledger = new DailyChallengeLedger();
        private string status = "Submit an event to begin.";
        private int previousDay;
        private int previousEvent;
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle notice;
        private GUIStyle button;

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            float panelWidth = Mathf.Min(800f, Screen.width - 32f);
            GUILayout.BeginArea(new Rect((Screen.width - panelWidth) * 0.5f, 14f, panelWidth, Screen.height - 28f));
            scroll = GUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            float contentWidth = panelWidth - GUI.skin.verticalScrollbar.fixedWidth - 8f;
            GUILayout.BeginVertical(GUILayout.Width(contentWidth));
            GUILayout.Label("Daily challenge", title);
            GUILayout.Label("Submit three events to unlock a reward. Only the simulated day button moves time.", body);
            GUILayout.Space(8f);
            GUILayout.Label($"Simulated day {ledger.Day}", heading);
            GUILayout.Label($"Progress {ledger.Progress} / {DailyChallengeLedger.Goal}", heading);
            DrawProgress();
            GUILayout.Label(ledger.MilestoneReached
                ? "Milestone reached"
                : $"Milestone at {DailyChallengeLedger.Milestone} events", body);

            string reward = ledger.AwardedToday ? "Claimed" :
                ledger.Progress >= DailyChallengeLedger.Goal ? "Ready to claim" : "Locked";
            GUILayout.Label($"Today's reward: {reward}", body);
            GUILayout.Label($"Total rewards: {ledger.TotalRewards}", body);
            GUILayout.Space(12f);
            DrawActions(contentWidth < 540f);
            GUILayout.Space(12f);
            GUILayout.Label(status, notice);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawActions(bool stack)
        {
            if (!stack)
                GUILayout.BeginHorizontal();
            bool initiallyEnabled = GUI.enabled;
            GUI.enabled = initiallyEnabled && ledger.Progress < DailyChallengeLedger.Goal;
            if (GUILayout.Button("Submit event", button, GUILayout.Height(42f)))
                Submit(ledger.Day, ledger.Progress + 1);
            GUI.enabled = initiallyEnabled && previousEvent != 0;
            if (GUILayout.Button("Retry previous event", button, GUILayout.Height(42f)))
                Submit(previousDay, previousEvent);
            GUI.enabled = initiallyEnabled;
            if (!stack)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
            if (GUILayout.Button("Claim reward", button, GUILayout.Height(42f)))
                Claim();
            if (GUILayout.Button("Next simulated day", button, GUILayout.Height(42f)) && ledger.AdvanceTo(ledger.Day + 1))
                status = $"Day {ledger.Day} started with zero progress. The reward total is still {ledger.TotalRewards}.";
            if (!stack)
                GUILayout.EndHorizontal();
        }

        private void Submit(int day, int eventId)
        {
            SubmissionOutcome outcome = ledger.Submit(day, eventId);
            if (outcome == SubmissionOutcome.Accepted)
            {
                previousDay = day;
                previousEvent = eventId;
            }
            status = outcome switch
            {
                SubmissionOutcome.Accepted => $"Accepted event {eventId} for day {day}.",
                SubmissionOutcome.Duplicate => $"Event {eventId} was already counted. Progress stayed at {ledger.Progress}.",
                SubmissionOutcome.WrongDay => $"Event {eventId} belongs to day {day}. Day {ledger.Day} still has {ledger.Progress} accepted events.",
                SubmissionOutcome.GoalComplete => "The current challenge is complete.",
                _ => "The event ID must be positive."
            };
        }

        private void Claim()
        {
            status = ledger.Claim(ledger.Day) switch
            {
                ClaimOutcome.Awarded => $"Day {ledger.Day} awarded one reward.",
                ClaimOutcome.AlreadyAwarded => $"Day {ledger.Day} was already claimed. The reward total is still {ledger.TotalRewards}.",
                _ => $"Complete all {DailyChallengeLedger.Goal} events before claiming."
            };
        }

        private void DrawProgress()
        {
            Rect track = GUILayoutUtility.GetRect(20f, 10f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            Color precedingColor = GUI.color;
            float segment = track.width / DailyChallengeLedger.Goal;
            for (int index = 0; index < DailyChallengeLedger.Goal; index++)
            {
                GUI.color = index < ledger.Progress ? Mint : Slot;
                Rect bar = new Rect(track.x + index * segment, track.y, segment - 4f, track.height);
                GUI.DrawTexture(bar, Texture2D.whiteTexture);
            }
            GUI.color = precedingColor;
        }

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = true };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, wordWrap = true };
            body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            notice = new GUIStyle(body);
            title.normal.textColor = Ink;
            heading.normal.textColor = Mint;
            body.normal.textColor = Ink;
            notice.normal.textColor = Amber;
        }
    }
}
