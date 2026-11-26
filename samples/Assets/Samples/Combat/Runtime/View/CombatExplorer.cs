using System;
using UnityEngine;

namespace GameplaySamples.Combat
{
    public sealed class CombatExplorer : MonoBehaviour
    {
        private const int MARGIN = 20;

        private static readonly Color Ink = new Color32(17, 25, 29, 255);
        private static readonly Color Paper = new Color32(237, 239, 225, 255);
        private static readonly Color Muted = new Color32(185, 199, 193, 255);
        private static readonly Color Mint = new Color32(133, 218, 183, 255);

        private Vector2 scrollPosition;
        private GUIStyle title;
        private GUIStyle heading;
        private GUIStyle body;
        private GUIStyle emphasis;
        private GUIStyle button;
        private GUIStyle panel;
        private Action nextInteraction;
        private bool showOpponent;
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
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);
            GUILayout.Label("Turn-based combat", title);
            GUILayout.Label("Step through card effects and an opponent's choices.", body);
            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            DrawTab("End-phase effects", false);
            DrawTab("Opponent choices", true);
            GUILayout.EndHorizontal();
            GUILayout.Space(12);

            if (showOpponent)
            {
                DrawOpponent();
            }
            else
            {
                DrawPlayer();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTab(string label, bool opponent)
        {
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = showOpponent == opponent ? Mint : Color.white;
            if (GUILayout.Button(label, button, GUILayout.Height(44)))
            {
                nextInteraction = () =>
                {
                    showOpponent = opponent;
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
