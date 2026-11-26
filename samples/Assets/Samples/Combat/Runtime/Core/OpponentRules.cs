using System;

namespace GameplaySamples.Combat
{
    public static class OpponentRules
    {
        public const int ATTACK_COST = 3;
        public const int STATUS_COST = 2;

        public enum Action
        {
            Attack,
            ImproveStatus,
            Wait,
            Swap,
            None
        }

        public enum Availability
        {
            Ready,
            AfterRecovery,
            BeyondCapacity,
            NeedsMoreMana,
            AlreadyAtLimit
        }

        public readonly struct Decision
        {
            public Action Chosen { get; }
            public Availability Attack { get; }
            public Availability Status { get; }
            public int NextMana { get; }
            public bool CanSwap { get; }

            internal Decision(Action chosen, Availability attack, Availability status,
                int nextMana, bool canSwap)
            {
                Chosen = chosen;
                Attack = attack;
                Status = status;
                NextMana = nextMana;
                CanSwap = canSwap;
            }
        }

        /// <summary>Chooses a useful, affordable move or looks ahead by one recovery step.</summary>
        public static Decision Choose(int mana, int maxMana, int recovery, bool statusAtLimit,
            int livingReserves)
        {
            if (mana < 0 || mana > maxMana || recovery < 0 || livingReserves < 0)
            {
                throw new ArgumentException("Mana, recovery and reserve counts must be valid.");
            }

            int nextMana = mana + Math.Min(recovery, maxMana - mana);
            Availability attack = CheckCost(ATTACK_COST, mana, nextMana, maxMana);
            Availability status = statusAtLimit ? Availability.AlreadyAtLimit
                : CheckCost(STATUS_COST, mana, nextMana, maxMana);
            bool canSwap = livingReserves > 0;
            Action chosen = PickAction(attack, status, canSwap);
            return new Decision(chosen, attack, status, nextMana, canSwap);
        }

        private static Action PickAction(Availability attack, Availability status, bool canSwap)
        {
            if (attack == Availability.Ready)
            {
                return Action.Attack;
            }
            if (status == Availability.Ready)
            {
                return Action.ImproveStatus;
            }
            // Wait only when the next turn changes what the opponent can do.
            if (attack == Availability.AfterRecovery || status == Availability.AfterRecovery)
            {
                return Action.Wait;
            }
            return canSwap ? Action.Swap : Action.None;
        }

        private static Availability CheckCost(int cost, int mana, int nextMana, int maxMana)
        {
            if (mana >= cost)
            {
                return Availability.Ready;
            }
            if (maxMana < cost)
            {
                return Availability.BeyondCapacity;
            }
            return nextMana >= cost ? Availability.AfterRecovery : Availability.NeedsMoreMana;
        }
    }
}
