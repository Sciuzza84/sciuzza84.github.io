using System;
using System.Collections.Generic;

namespace GameplaySamples.Combat
{
    public static class EndPhaseRules
    {
        public const int STATUS_DAMAGE = 2;
        public const int CARD_COST = 2;

        private static readonly IReadOnlyList<string> StartingDeck =
            Array.AsReadOnly(new[] { "Ward", "Feint", "Torch" });

        public enum Step
        {
            CardRejected,
            CardPlayed,
            CardReturnedToDeck,
            StatusApplied,
            PlayerDefeated,
            EnemyResponded
        }

        public readonly struct Result
        {
            public int Health { get; }
            public int Energy { get; }
            public IReadOnlyList<Step> Steps { get; }
            public IReadOnlyList<string> Deck { get; }

            internal Result(int health, int energy, IReadOnlyList<Step> steps, IReadOnlyList<string> deck)
            {
                Health = health;
                Energy = energy;
                Steps = steps;
                Deck = deck;
            }
        }

        /// <summary>Resolves a single player phase and at most one enemy response.</summary>
        public static Result Resolve(int health, int energy, bool playCard)
        {
            if (health <= 0 || energy < 0)
            {
                throw new ArgumentException("An active phase needs positive health and nonnegative energy.");
            }

            if (playCard && energy < CARD_COST)
            {
                var rejected = Array.AsReadOnly(new[] { Step.CardRejected });
                return new Result(health, energy, rejected, StartingDeck);
            }

            var steps = new List<Step>(4);
            IReadOnlyList<string> deck = StartingDeck;
            if (playCard)
            {
                energy -= CARD_COST;
                steps.Add(Step.CardPlayed);
                deck = Array.AsReadOnly(new[] { StartingDeck[1], StartingDeck[2], StartingDeck[0] });
                steps.Add(Step.CardReturnedToDeck);
            }

            health = Math.Max(0, health - STATUS_DAMAGE);
            steps.Add(Step.StatusApplied);
            // The end-of-phase effect resolves before either side can act again.
            steps.Add(health == 0 ? Step.PlayerDefeated : Step.EnemyResponded);
            return new Result(health, energy, steps.AsReadOnly(), deck);
        }
    }
}
