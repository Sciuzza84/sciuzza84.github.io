using System;
using System.Collections.Generic;
using System.Linq;

namespace GameplaySamples.RoundPlanner
{
    public readonly struct RoundItem
    {
        public RoundItem(string family, string variant)
        {
            Family = family;
            Variant = variant;
        }

        public string Family { get; }
        public string Variant { get; }

        public override string ToString() => $"{Family} / {Variant}";
    }

    public sealed class RoundPlan
    {
        internal RoundPlan(int number, RoundItem[] targets, RoundItem[] distractors)
        {
            Number = number;
            Targets = Array.AsReadOnly(targets);
            Distractors = Array.AsReadOnly(distractors);
        }

        public int Number { get; }
        public IReadOnlyList<RoundItem> Targets { get; }
        public IReadOnlyList<RoundItem> Distractors { get; }
    }

    public sealed class RoundPlanner
    {
        private readonly ItemFamily[] families;

        public RoundPlanner(IEnumerable<RoundItem> catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            RoundItem[] items = catalog.ToArray();
            if (items.Length == 0 || items.Any(item =>
                    string.IsNullOrWhiteSpace(item.Family) || string.IsNullOrWhiteSpace(item.Variant)))
                throw new ArgumentException("The catalog needs named families and variants.", nameof(catalog));
            if (items.Select(item => (item.Family, item.Variant)).Distinct().Count() != items.Length)
                throw new ArgumentException("Each family and variant pair must be unique.", nameof(catalog));

            families = items.GroupBy(item => item.Family, StringComparer.Ordinal)
                .Select(group => new ItemFamily(group.ToArray())).ToArray();
            Families = Array.AsReadOnly(families.Select(family => family.Name).ToArray());
        }

        public IReadOnlyList<string> Families { get; }
        public RoundPlan Accepted { get; private set; }

        public int TargetUses(string family) => families.First(entry => entry.Name == family).TargetUses;

        public bool TryAccept(int targetCount, int distractorCount, out string error)
        {
            if (targetCount < 1 || distractorCount < 0)
            {
                error = "Choose at least one target and zero or more distractors.";
                return false;
            }
            if (targetCount > families.Length || distractorCount > families.Length - targetCount)
            {
                error = $"Quota needs more than the {families.Length} distinct families available.";
                return false;
            }

            // OrderBy is stable: equal usage follows the catalog order.
            ItemFamily[] ranked = families.OrderBy(family => family.TargetUses).ToArray();
            var targets = new RoundItem[targetCount];
            var variants = new int[targetCount];
            for (int index = 0; index < targetCount; index++)
            {
                ItemFamily family = ranked[index];
                variants[index] = family.LeastUsedVariant();
                targets[index] = family.Variants[variants[index]];
            }

            var distractors = new RoundItem[distractorCount];
            for (int index = 0; index < distractorCount; index++)
            {
                ItemFamily family = ranked[targetCount + index];
                distractors[index] = family.Variants[family.LeastUsedVariant()];
            }

            var next = new RoundPlan((Accepted?.Number ?? 0) + 1, targets, distractors);
            for (int index = 0; index < targetCount; index++)
                ranked[index].RecordTarget(variants[index]);
            Accepted = next;
            error = null;
            return true;
        }

        public static RoundPlanner CreateDemo() => new RoundPlanner(new[]
        {
            new RoundItem("Compass", "brass"), new RoundItem("Compass", "blue"),
            new RoundItem("Lantern", "paper"), new RoundItem("Lantern", "glass"),
            new RoundItem("Key", "iron"), new RoundItem("Key", "copper"),
            new RoundItem("Shell", "striped"), new RoundItem("Shell", "cream"),
            new RoundItem("Kite", "linen"), new RoundItem("Kite", "red"),
            new RoundItem("Clock", "round"), new RoundItem("Clock", "square")
        });

        private sealed class ItemFamily
        {
            private readonly int[] variantUses;

            public ItemFamily(RoundItem[] variants)
            {
                Variants = variants;
                variantUses = new int[variants.Length];
            }

            public string Name => Variants[0].Family;
            public RoundItem[] Variants { get; }
            public int TargetUses { get; private set; }

            public int LeastUsedVariant()
            {
                int best = 0;
                for (int index = 1; index < variantUses.Length; index++)
                {
                    if (variantUses[index] < variantUses[best])
                        best = index;
                }
                return best;
            }

            public void RecordTarget(int variant)
            {
                TargetUses++;
                variantUses[variant]++;
            }
        }
    }
}
