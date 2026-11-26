using GameplaySamples.DailyChallenge;

var challenge = new DailyChallengeLedger();
Check(challenge.Day == 1 && challenge.Progress == 0 && challenge.TotalRewards == 0, "initial day");
Check(challenge.Claim(1) == ClaimOutcome.Incomplete, "early claim is rejected");
Check(challenge.Submit(2, 1) == SubmissionOutcome.WrongDay && challenge.Submit(1, 0) == SubmissionOutcome.InvalidId &&
      challenge.Progress == 0, "future and invalid events leave progress unchanged");

Check(challenge.Submit(1, 1) == SubmissionOutcome.Accepted && challenge.Progress == 1, "first submission");
Check(challenge.Submit(1, 1) == SubmissionOutcome.Duplicate && challenge.Progress == 1,
    "same event contributes once");
Check(!challenge.MilestoneReached && challenge.Submit(1, 2) == SubmissionOutcome.Accepted &&
      challenge.MilestoneReached && challenge.Claim(1) == ClaimOutcome.Incomplete, "milestone precedes reward");
Check(challenge.Submit(1, 3) == SubmissionOutcome.Accepted && challenge.Progress == DailyChallengeLedger.Goal,
    "goal reached");
Check(challenge.Submit(1, 4) == SubmissionOutcome.GoalComplete &&
      challenge.Progress == DailyChallengeLedger.Goal, "goal caps progress");
Check(challenge.Claim(1) == ClaimOutcome.Awarded && challenge.AwardedToday && challenge.TotalRewards == 1,
    "reward claim");
Check(challenge.Claim(1) == ClaimOutcome.AlreadyAwarded && challenge.TotalRewards == 1, "claim retry is idempotent");
Check(!challenge.AdvanceTo(1) && !challenge.AdvanceTo(0) && challenge.AwardedToday && challenge.TotalRewards == 1,
    "repeating or moving back a day cannot reset the claim");

Check(challenge.AdvanceTo(2) && challenge.Day == 2 && challenge.Progress == 0 &&
      !challenge.MilestoneReached && !challenge.AwardedToday && challenge.TotalRewards == 1, "new day resets daily state");
Check(challenge.Submit(1, 3) == SubmissionOutcome.WrongDay && challenge.Claim(1) == ClaimOutcome.WrongDay &&
      challenge.Progress == 0 && challenge.TotalRewards == 1, "old day operations cannot mutate the new day");
Check(challenge.Submit(2, 1) == SubmissionOutcome.Accepted && challenge.Progress == 1,
    "the same event number on a new day is independent");
Check(challenge.Submit(2, 2) == SubmissionOutcome.Accepted && challenge.Submit(2, 3) == SubmissionOutcome.Accepted &&
      challenge.Claim(2) == ClaimOutcome.Awarded && challenge.TotalRewards == 2,
    "a second day can claim an independent reward");
Check(challenge.Claim(2) == ClaimOutcome.AlreadyAwarded && challenge.TotalRewards == 2,
    "the second day still grants once");

Console.WriteLine("PASS: milestone progress, duplicate and stale events, once-per-day claims, day reset, independent new-day rewards.");

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"Daily challenge check failed: {name}");
}
