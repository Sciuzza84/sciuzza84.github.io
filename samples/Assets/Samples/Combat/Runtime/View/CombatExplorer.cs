using System;
using UnityEngine;

namespace GameplaySamples.Combat
{
    public sealed class CombatExplorer : MonoBehaviour
    {
        private enum Scenario { EndPhase, Opponent, Feedback }

        private const int MARGIN = 20;
        private const float StackedControlsWidth = 300f;

        private static readonly Color Ink = new Color32(17, 25, 29, 255);
        private static readonly Color Paper = new Color32(237, 239, 225, 255);
        private static readonly Color Muted = new Color32(185, 199, 193, 255);
        private static readonly Color Mint = new Color32(133, 218, 183, 255);
        private static readonly Color Coral = new Color32(245, 150, 145, 255);

        private Vector2 scrollPosition;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle emphasis;
        private GUIStyle button;
        private GUIStyle panel;
        private GUIStyle damageLabel;
        private GUIStyle healLabel;
        private Action nextInteraction;
        private Scenario scenario;
        private FeedbackPool feedback = FeedbackBurst.CreatePool();
        private bool playerPhaseComplete;
        private bool opponentFinished;
        private int playerHealth;
        private int playerEnergy;
        private int mana;
        private int capacity;
        private int recovery;
        private int livingReserves;
        private bool statusAtLimit;
        private string deck;
        private string playerTrace;
        private string opponentTrace;
        private string lastOpponentAction;
        private int opponentTurns;

        private void Awake()
        {
            SetPlayerPreset(2, 2);
            SetOpponentPreset(1, 4, 2);
        }

        private void Update()
        {
            Action interaction = nextInteraction;
            nextInteraction = null;
            interaction?.Invoke();
        }

        private void OnGUI()
        {
            if (title == null)
            {
                CreateStyles();
            }

            Color originalColor = GUI.color;
            GUI.color = Ink;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = originalColor;

            int width = Mathf.Max(1, Mathf.Min(820, Screen.width - MARGIN * 2));
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, MARGIN, width,
                Mathf.Max(1, Screen.height - MARGIN * 2)));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            float contentWidth = Mathf.Max(1f, width - GUI.skin.verticalScrollbar.fixedWidth - 8f);
            bool stackControls = contentWidth < StackedControlsWidth;
            GUILayout.BeginVertical(GUILayout.Width(contentWidth));
            GUILayout.Label("Turn-based combat", title);
            GUILayout.Label("Card effects, opponent choices and floating feedback.", body);
            GUILayout.Space(16);

            if (!stackControls)
                GUILayout.BeginHorizontal();
            float tabWidth = Mathf.Max(1f, stackControls ? contentWidth - 8f : (contentWidth - 12f) / 3f);
            float tabHeight = !stackControls && contentWidth < 420f ? 52f : 44f;
            DrawTab("End-phase effects", Scenario.EndPhase, tabWidth, tabHeight);
            DrawTab("Opponent choices", Scenario.Opponent, tabWidth, tabHeight);
            DrawTab("Feedback pool", Scenario.Feedback, tabWidth, tabHeight);
            if (!stackControls)
                GUILayout.EndHorizontal();
            GUILayout.Space(12);

            switch (scenario)
            {
                case Scenario.Opponent:
                    DrawOpponent();
                    break;
                case Scenario.Feedback:
                    DrawFeedback(stackControls);
                    break;
                default:
                    DrawPlayer();
                    break;
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTab(string label, Scenario selection, float width, float height)
        {
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = scenario == selection ? Mint : Color.white;
            if (GUILayout.Button(label, button, GUILayout.Width(width), GUILayout.Height(height)))
            {
                nextInteraction = () =>
                {
                    scenario = selection;
                    scrollPosition = Vector2.zero;
                };
            }
            GUI.backgroundColor = oldBackground;
        }

        private void DrawPlayer()
        {
            GUILayout.BeginVertical(panel);
            GUILayout.Label("The effect resolves before the enemy acts", heading);
            GUILayout.Label("A status effect deals 2 damage at the end of the player phase.", body);
            bool wide = Screen.width >= 660;
            if (wide)
            {
                GUILayout.BeginHorizontal();
            }
            DrawAction("2 health · Defeat", true, () => SetPlayerPreset(2, 2));
            DrawAction("3 health · Survive", true, () => SetPlayerPreset(3, 2));
            DrawAction("1 energy · Card rejected", true, () => SetPlayerPreset(3, 1));
            if (wide)
            {
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(9);
            GUILayout.Label($"Health {playerHealth}    Energy {playerEnergy}", emphasis);
            GUILayout.Label("Deck order    " + deck, body);
            DrawAction("Play Ward and end phase (2 energy)", !playerPhaseComplete, () => ResolvePlayer(true));
            DrawAction("End phase without a card", !playerPhaseComplete, () => ResolvePlayer(false));
            GUILayout.Space(8);
            GUILayout.Label(playerTrace, body);
            GUILayout.EndVertical();
        }

        private void DrawOpponent()
        {
            GUILayout.BeginVertical(panel);
            GUILayout.Label("The opponent looks one turn ahead", heading);
            GUILayout.Label("Attack costs 3 mana. A useful status move costs 2.", body);
            DrawAction("Attack now", true, () => SetOpponentPreset(3, 4, 2));
            DrawAction("Recover enough mana next turn", true, () => SetOpponentPreset(1, 4, 2));
            DrawAction("Switch to a living reserve", true, () => SetOpponentPreset(1, 2, 1, reserves: 1));
            DrawAction("No useful action", true, () => SetOpponentPreset(1, 2, 1));
            DrawAction("Use an uncapped status move", true, () => SetOpponentPreset(2, 2, 1, capped: false));

            GUILayout.Space(9);
            string status = statusAtLimit ? "Capped" : "Can improve";
            GUILayout.Label($"Mana {mana}/{capacity}    Recovery {recovery}    Living reserves {livingReserves}",
                emphasis);
            GUILayout.Label("Status    " + status, body);
            DrawAction("Resolve opponent action", !opponentFinished, ResolveOpponent);
            GUILayout.Space(8);
            GUILayout.Label(opponentTrace, body);
            GUILayout.EndVertical();
        }

        private void DrawFeedback(bool stackControls)
        {
            GUILayout.BeginVertical(panel);
            GUILayout.Label("Pool floating combat labels", heading);
            GUILayout.Label("Eight events arrive at once. Nearby hits merge with hits, and heals with heals.", body);
            if (!stackControls)
                GUILayout.BeginHorizontal();
            DrawAction("Send burst", true, () => FeedbackBurst.Send(feedback));
            DrawAction("Advance tick", true, () => feedback.Advance());
            DrawAction("Reset", true, () => feedback = FeedbackBurst.CreatePool());
            if (!stackControls)
                GUILayout.EndHorizontal();
            GUILayout.Space(8);

            int active = DrawFeedbackArena();
            GUILayout.Label($"Active {active}/{feedback.Capacity}     Tick {feedback.Tick}", emphasis);
            GUILayout.Label($"Merged {feedback.Merged}   Reused {feedback.Reused}   Dropped {feedback.Dropped}", body);
            GUILayout.Label($"Labels expire after {feedback.LifetimeTicks} ticks. Send another burst to reuse freed slots.", body);
            GUILayout.EndVertical();
        }

        private int DrawFeedbackArena()
        {
            Rect bounds = GUILayoutUtility.GetRect(1f, 124f, GUILayout.ExpandWidth(true));
            GUI.Box(bounds, GUIContent.none);
            int active = 0;
            for (int index = 0; index < feedback.Capacity; index++)
            {
                if (!feedback.TryGetActive(index, out FeedbackLabel label))
                    continue;

                active++;
                if (Event.current.type != EventType.Repaint)
                    continue;

                bool heal = label.Kind == FeedbackKind.Heal;
                long age = feedback.Tick - (label.ExpiresAt - feedback.LifetimeTicks);
                float x = Mathf.Lerp(bounds.xMin + 37f, bounds.xMax - 37f, label.X / 100f);
                float y = Mathf.Lerp(bounds.yMin + 29f, bounds.yMax - 29f, label.Y / 100f) + (heal ? 14f : -14f) - age * 4f;
                GUI.Label(new Rect(x - 37f, y - 11f, 74f, 22f),
                    $"#{index + 1}  {(heal ? "+" : "-")}{label.Amount}", heal ? healLabel : damageLabel);
            }
            if (active == 0 && Event.current.type == EventType.Repaint)
                GUI.Label(new Rect(bounds.x + 12f, bounds.y + 43f, bounds.width - 24f, 38f), "All slots available", body);
            return active;
        }

        private void DrawAction(string label, bool enabled, Action interaction)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && enabled;
            if (GUILayout.Button(label, button, GUILayout.MinHeight(36)))
            {
                nextInteraction = interaction;
            }
            GUI.enabled = wasEnabled;
        }

        private void SetPlayerPreset(int health, int energy)
        {
            playerHealth = health;
            playerEnergy = energy;
            playerPhaseComplete = false;
            deck = "Ward  /  Feint  /  Torch";
            playerTrace = "Choose how to end the player phase.";
        }

        private void ResolvePlayer(bool playCard)
        {
            EndPhaseRules.Result result = EndPhaseRules.Resolve(playerHealth, playerEnergy, playCard);
            playerHealth = result.Health;
            playerEnergy = result.Energy;
            deck = string.Join("  /  ", result.Deck);
            playerPhaseComplete = result.Steps[0] != EndPhaseRules.Step.CardRejected;
            var lines = new string[result.Steps.Count];
            for (int index = 0; index < lines.Length; index++)
            {
                lines[index] = $"{index + 1}. {DescribeStep(result.Steps[index])}";
            }
            playerTrace = string.Join("\n", lines);
        }

        private static string DescribeStep(EndPhaseRules.Step step)
        {
            switch (step)
            {
                case EndPhaseRules.Step.CardRejected:
                    return "Ward needs 2 energy. The phase remains open.";
                case EndPhaseRules.Step.CardPlayed:
                    return "Played Ward and spent 2 energy.";
                case EndPhaseRules.Step.CardReturnedToDeck:
                    return "Moved Ward to the back of the deck.";
                case EndPhaseRules.Step.StatusApplied:
                    return "End-of-phase status dealt 2 damage.";
                case EndPhaseRules.Step.PlayerDefeated:
                    return "Player defeated. The enemy response was canceled.";
                default:
                    return "The enemy responded once.";
            }
        }

        private void SetOpponentPreset(int startingMana, int manaCapacity, int recoveryPerTurn,
            int reserves = 0, bool capped = true)
        {
            mana = startingMana;
            capacity = manaCapacity;
            recovery = recoveryPerTurn;
            statusAtLimit = capped;
            livingReserves = reserves;
            opponentTurns = 0;
            opponentFinished = false;
            lastOpponentAction = string.Empty;
            opponentTrace = "Resolve an action to see which options were available.";
        }

        private void ResolveOpponent()
        {
            OpponentRules.Decision decision =
                OpponentRules.Choose(mana, capacity, recovery, statusAtLimit, livingReserves);
            string turn = $"Turn {++opponentTurns}     {ApplyDecision(decision)}";
            string reserve = decision.CanSwap ? "Living reserve available" : "No living reserve";
            opponentTrace = $"{lastOpponentAction}{turn}\nAttack: {DescribeAvailability(decision.Attack)}."
                + $"\nStatus: {DescribeAvailability(decision.Status)}.\nSwap: {reserve}.";
            lastOpponentAction = turn + "\n";
        }

        private string ApplyDecision(OpponentRules.Decision decision)
        {
            switch (decision.Chosen)
            {
                case OpponentRules.Action.Attack:
                    mana -= OpponentRules.ATTACK_COST;
                    return "Attack. Spent 3 mana.";
                case OpponentRules.Action.ImproveStatus:
                    mana -= OpponentRules.STATUS_COST;
                    statusAtLimit = true;
                    return "Status improved. Spent 2 mana.";
                case OpponentRules.Action.Wait:
                    mana = decision.NextMana;
                    return $"Wait. The next turn starts with {mana} mana.";
                case OpponentRules.Action.Swap:
                    livingReserves--;
                    opponentFinished = true;
                    return "Swapped to a living reserve.";
                default:
                    opponentFinished = true;
                    return "No useful action available.";
            }
        }

        private static string DescribeAvailability(OpponentRules.Availability availability)
        {
            switch (availability)
            {
                case OpponentRules.Availability.Ready:
                    return "Affordable now";
                case OpponentRules.Availability.AfterRecovery:
                    return "Affordable on the next turn";
                case OpponentRules.Availability.BeyondCapacity:
                    return "Cost exceeds mana capacity";
                case OpponentRules.Availability.AlreadyAtLimit:
                    return "The statistic is at its limit";
                default:
                    return "Not enough mana by the next turn";
            }
        }

        private void CreateStyles()
        {
            title = MakeLabel(34, Paper, FontStyle.Bold);
            heading = MakeLabel(20, Paper, FontStyle.Bold);
            body = MakeLabel(15, Muted, FontStyle.Normal);
            emphasis = MakeLabel(16, Mint, FontStyle.Bold);
            damageLabel = MakeLabel(17, Coral, FontStyle.Bold);
            healLabel = MakeLabel(17, Mint, FontStyle.Bold);
            damageLabel.alignment = healLabel.alignment = TextAnchor.MiddleCenter;
            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(15, 15, 14, 14) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
        }

        private static GUIStyle MakeLabel(int fontSize, Color color, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true,
                fontStyle = fontStyle,
                normal = { textColor = color }
            };
        }
    }
}
