using System.Collections.Generic;

namespace GameplaySamples.DailyChallenge
{
    public enum SubmissionOutcome
    {
        Accepted,
        Duplicate,
        WrongDay,
        InvalidId,
        GoalComplete
    }

    public enum ClaimOutcome
    {
        Awarded,
        AlreadyAwarded,
        Incomplete,
        WrongDay
    }

    public sealed class DailyChallengeLedger
    {
        public const int Milestone = 2;
        public const int Goal = 3;
        private readonly HashSet<int> acceptedEvents = new HashSet<int>();

        public int Day { get; private set; } = 1;
        public int Progress => acceptedEvents.Count;
        public bool MilestoneReached => Progress >= Milestone;
        public bool AwardedToday { get; private set; }
        public int TotalRewards { get; private set; }

        public SubmissionOutcome Submit(int day, int eventId)
        {
            if (day != Day)
                return SubmissionOutcome.WrongDay;
            if (eventId <= 0)
                return SubmissionOutcome.InvalidId;
            if (acceptedEvents.Contains(eventId))
                return SubmissionOutcome.Duplicate;
            if (Progress >= Goal)
                return SubmissionOutcome.GoalComplete;

            acceptedEvents.Add(eventId);
            return SubmissionOutcome.Accepted;
        }

        public ClaimOutcome Claim(int day)
        {
            if (day != Day)
                return ClaimOutcome.WrongDay;
            if (AwardedToday)
                return ClaimOutcome.AlreadyAwarded;
            if (Progress < Goal)
                return ClaimOutcome.Incomplete;

            AwardedToday = true;
            TotalRewards++;
            return ClaimOutcome.Awarded;
        }

        public bool AdvanceTo(int nextDay)
        {
            if (nextDay <= Day)
                return false;

            Day = nextDay;
            acceptedEvents.Clear();
            AwardedToday = false;
            return true;
        }
    }
}
