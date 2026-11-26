using System;

namespace GameplaySamples.Combat
{
    internal enum FeedbackKind { Damage, Heal }

    internal enum FeedbackOutcome { Started, Merged, Reused, Dropped }

    internal readonly struct FeedbackLabel
    {
        internal FeedbackLabel(FeedbackKind kind, int x, int y, long amount, long expiresAt)
        {
            Kind = kind;
            X = x;
            Y = y;
            Amount = amount;
            ExpiresAt = expiresAt;
        }

        internal FeedbackKind Kind { get; }
        internal int X { get; }
        internal int Y { get; }
        internal long Amount { get; }
        internal long ExpiresAt { get; }
    }

    internal sealed class FeedbackPool
    {
        private readonly FeedbackLabel[] slots;
        private readonly int mergeRadius;
        private readonly long radiusSquared;

        internal FeedbackPool(int capacity, int lifetimeTicks, int mergeRadius)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (lifetimeTicks < 1)
                throw new ArgumentOutOfRangeException(nameof(lifetimeTicks));
            if (mergeRadius < 0)
                throw new ArgumentOutOfRangeException(nameof(mergeRadius));

            slots = new FeedbackLabel[capacity];
            LifetimeTicks = lifetimeTicks;
            this.mergeRadius = mergeRadius;
            radiusSquared = (long)mergeRadius * mergeRadius;
        }

        internal int Capacity => slots.Length;
        internal int LifetimeTicks { get; }
        internal long Tick { get; private set; }
        internal long Started { get; private set; }
        internal long Merged { get; private set; }
        internal long Reused { get; private set; }
        internal long Dropped { get; private set; }

        internal bool TryGetActive(int index, out FeedbackLabel label)
        {
            label = slots[index];
            return label.ExpiresAt > Tick;
        }

        internal void Advance() => Tick = checked(Tick + 1);

        internal FeedbackOutcome Emit(FeedbackKind kind, int x, int y, int amount)
        {
            if (kind != FeedbackKind.Damage && kind != FeedbackKind.Heal)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (amount < 1)
                throw new ArgumentOutOfRangeException(nameof(amount));

            int freeSlot = -1;
            int nearestMatch = -1;
            long nearestDistance = long.MaxValue;
            for (int index = 0; index < slots.Length; index++)
            {
                if (!TryGetActive(index, out FeedbackLabel label))
                {
                    if (freeSlot == -1)
                        freeSlot = index;
                    continue;
                }
                if (label.Kind != kind)
                    continue;

                long dx = (long)label.X - x;
                long dy = (long)label.Y - y;
                if (Math.Abs(dx) > mergeRadius || Math.Abs(dy) > mergeRadius)
                    continue;

                long distance = dx * dx + dy * dy;
                if (distance <= radiusSquared && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestMatch = index;
                }
            }

            if (nearestMatch >= 0)
            {
                FeedbackLabel label = slots[nearestMatch];
                // A burst keeps the original anchor and expiry. Even a busy slot becomes free on time.
                slots[nearestMatch] = new FeedbackLabel(kind, label.X, label.Y,
                    checked(label.Amount + amount), label.ExpiresAt);
                Merged++;
                return FeedbackOutcome.Merged;
            }
            if (freeSlot < 0)
            {
                Dropped++;
                return FeedbackOutcome.Dropped;
            }

            bool recycled = slots[freeSlot].ExpiresAt != 0;
            slots[freeSlot] = new FeedbackLabel(kind, x, y, amount, checked(Tick + LifetimeTicks));
            if (recycled)
            {
                Reused++;
                return FeedbackOutcome.Reused;
            }

            Started++;
            return FeedbackOutcome.Started;
        }
    }

    internal static class FeedbackBurst
    {
        internal static FeedbackPool CreatePool() => new FeedbackPool(capacity: 4, lifetimeTicks: 3, mergeRadius: 10);

        internal static void Send(FeedbackPool pool)
        {
            pool.Emit(FeedbackKind.Damage, 20, 38, 5);
            pool.Emit(FeedbackKind.Damage, 25, 40, 7);
            pool.Emit(FeedbackKind.Heal, 20, 38, 4);
            pool.Emit(FeedbackKind.Heal, 23, 39, 3);
            pool.Emit(FeedbackKind.Damage, 50, 58, 8);
            pool.Emit(FeedbackKind.Damage, 78, 38, 9);
            pool.Emit(FeedbackKind.Damage, 53, 61, 2);
            pool.Emit(FeedbackKind.Heal, 80, 40, 6);
        }
    }
}
