using System;
using System.Collections.Generic;
using System.Globalization;

namespace GameplaySamples.Dungeon
{
    internal sealed class DungeonBranchPreset
    {
        internal const int ChoiceCount = 3;

        private readonly int[] roomCounts;
        private readonly int[] cumulativeWeights;

        private DungeonBranchPreset(int[] roomCounts, int[] cumulativeWeights)
        {
            this.roomCounts = roomCounts;
            this.cumulativeWeights = cumulativeWeights;
        }

        internal static (string[] Rooms, string[] Weights) Example(bool invalid) => invalid
            ? (new[] { "2", "2", "4" }, new[] { "5", "5", "70" })
            : (new[] { "2", "3", "4" }, new[] { "20", "30", "50" });

        internal int LengthForRoll(int roll)
        {
            for (int index = 0; index < cumulativeWeights.Length - 1; index++)
            {
                if (roll < cumulativeWeights[index])
                    return roomCounts[index];
            }

            return roomCounts[roomCounts.Length - 1];
        }

        internal static DungeonPresetValidation Validate(IReadOnlyList<string> rooms, IReadOnlyList<string> weights)
        {
            if (rooms.Count != ChoiceCount || weights.Count != ChoiceCount)
                throw new ArgumentException($"A branch preset needs {ChoiceCount} room choices and weights.");

            var roomCounts = new int[ChoiceCount];
            var cumulativeWeights = new int[ChoiceCount];
            var roomErrors = new string[ChoiceCount];
            var weightErrors = new string[ChoiceCount];
            var firstRowByLength = new Dictionary<int, int>();
            bool validRooms = true;
            bool validWeights = true;
            int total = 0;
            for (int index = 0; index < ChoiceCount; index++)
            {
                if (!TryReadBetween(rooms[index], 2, 4, out int roomCount))
                    roomErrors[index] = "Use 2, 3 or 4 rooms.";
                else if (firstRowByLength.TryGetValue(roomCount, out int firstRow))
                    roomErrors[index] = $"Used in row {firstRow + 1}.\nPick another.";
                else
                    firstRowByLength.Add(roomCount, index);

                if (!TryReadBetween(weights[index], 0, 100, out int weight))
                {
                    weightErrors[index] = "Use a whole %\nfrom 0 to 100.";
                    validWeights = false;
                }
                else
                {
                    total += weight;
                }

                validRooms &= roomErrors[index] == null;
                roomCounts[index] = roomCount;
                cumulativeWeights[index] = total;
            }

            int difference = 100 - total;
            string weightFeedback = !validWeights ? "Fix the highlighted weights to check the total." : difference switch
            {
                > 0 => $"Total {total}%. Add {difference} points.",
                < 0 => $"Total {total}%. Remove {-difference} points.",
                _ => "Total 100%."
            };
            bool validTotal = validWeights && total == 100;
            var preset = validRooms && validTotal ? new DungeonBranchPreset(roomCounts, cumulativeWeights) : null;
            return new DungeonPresetValidation(preset, roomErrors, weightErrors, weightFeedback, validTotal);
        }

        private static bool TryReadBetween(string text, int min, int max, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;
    }

    internal sealed class DungeonPresetValidation
    {
        internal DungeonPresetValidation(DungeonBranchPreset preset, string[] roomErrors, string[] weightErrors,
            string weightFeedback, bool validWeightTotal)
        {
            Preset = preset;
            RoomErrors = roomErrors;
            WeightErrors = weightErrors;
            WeightFeedback = weightFeedback;
            ValidWeightTotal = validWeightTotal;
        }

        internal DungeonBranchPreset Preset { get; }
        internal IReadOnlyList<string> RoomErrors { get; }
        internal IReadOnlyList<string> WeightErrors { get; }
        internal string WeightFeedback { get; }
        internal bool ValidWeightTotal { get; }
    }
}
