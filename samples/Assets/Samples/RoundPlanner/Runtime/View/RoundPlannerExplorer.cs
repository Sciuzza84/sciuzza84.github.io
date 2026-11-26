using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameplaySamples.RoundPlanner
{
    public sealed class RoundPlannerExplorer : MonoBehaviour
    {
        private static readonly Color Ink = new Color32(233, 242, 236, 255);
        private static readonly Color Mint = new Color32(119, 222, 183, 255);
        private static readonly Color Amber = new Color32(244, 187, 113, 255);

        private readonly RoundPlanner planner = RoundPlanner.CreateDemo();
        private string status;
        private Vector2 scroll;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle text;
        private GUIStyle button;
        private GUIStyle notice;

        private void Awake()
        {
            Accept(2, 2);
        }

        private void OnGUI()
        {
            if (title == null)
                CreateStyles();

            float panelWidth = Mathf.Min(850f, Screen.width - 32f);
            bool stack = panelWidth < 610f;
            GUILayout.BeginArea(new Rect((Screen.width - panelWidth) * 0.5f, 14f, panelWidth, Screen.height - 28f));
            scroll = GUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
            float contentWidth = panelWidth - GUI.skin.verticalScrollbar.fixedWidth - 8f;
            GUILayout.BeginVertical(GUILayout.Width(contentWidth));
            GUILayout.Label("Round planner", title);
            GUILayout.Label("Plan rounds from a small catalog. Families and variants with fewer target appearances are picked first.", text);
            GUILayout.Space(12f);

            if (!stack)
                GUILayout.BeginHorizontal();
            if (GUILayout.Button("Accept next round", button, GUILayout.Height(38)))
                Accept(2, 2);
            if (GUILayout.Button("Try invalid quota", button, GUILayout.Height(38)))
                Accept(5, 2);
            if (GUILayout.Button("Replay accepted round", button, GUILayout.Height(38)))
                status = $"Replayed round {planner.Accepted.Number}. The plan and target counts are unchanged.";
            if (!stack)
                GUILayout.EndHorizontal();

            GUILayout.Label(status, notice);
            GUILayout.Space(8f);
            RoundPlan plan = planner.Accepted;
            GUILayout.Label($"Accepted round {plan.Number}", heading);
            DrawItems("Targets", plan.Targets);
            DrawItems("Distractors", plan.Distractors);

            GUILayout.Space(12f);
            GUILayout.Label("Appearances as a target", heading);
            for (int index = 0; index < planner.Families.Count; index += 2)
            {
                GUILayout.BeginHorizontal();
                DrawCount(planner.Families[index], contentWidth / 2f);
                if (index + 1 < planner.Families.Count)
                    DrawCount(planner.Families[index + 1], contentWidth / 2f);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Synthetic catalog. Distractors leave target counts unchanged.", text);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Accept(int targets, int distractors)
        {
            if (planner.TryAccept(targets, distractors, out string error))
                status = $"Round {planner.Accepted.Number} accepted. Target counts updated.";
            else
                status = $"{error} Kept round {planner.Accepted.Number} and its target counts.";
        }

        private void DrawItems(string label, IReadOnlyList<RoundItem> items)
        {
            string names = string.Join("   ·   ", items.Select(item => item.ToString()));
            GUILayout.Label($"{label}: {names}", text);
        }

        private void DrawCount(string family, float width)
        {
            GUILayout.Label($"{family}: {planner.TargetUses(family)}", text, GUILayout.Width(width - 4f));
        }

        private void CreateStyles()
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = true };
            heading = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold, wordWrap = true };
            text = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            notice = new GUIStyle(text);
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
            title.normal.textColor = Ink;
            heading.normal.textColor = Mint;
            text.normal.textColor = Ink;
            notice.normal.textColor = Amber;
        }
    }
}
