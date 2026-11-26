using GameplaySamples.RoundPlanner;

var planner = RoundPlanner.CreateDemo();
var replay = RoundPlanner.CreateDemo();
RoundPlan first = Accept(planner);
int[] firstCounts = planner.Families.Select(planner.TargetUses).ToArray();
Check(first.Number == 1 && TargetLabels(first) == "Compass / brass, Lantern / paper", "first round");
Check(first.Distractors.Select(item => item.Family).SequenceEqual(new[] { "Key", "Shell" }),
    "distractor selection");
Check(firstCounts.SequenceEqual(new[] { 1, 1, 0, 0, 0, 0 }), "only targets accrue usage");

foreach ((int targets, int distractors) in new[] { (5, 2), (0, 1), (1, -1), (int.MaxValue, int.MaxValue) })
{
    Check(!planner.TryAccept(targets, distractors, out string error) && !string.IsNullOrEmpty(error),
        "invalid quota reports an error");
    Check(ReferenceEquals(first, planner.Accepted) && firstCounts.SequenceEqual(planner.Families.Select(planner.TargetUses)),
        "invalid quota preserves the accepted plan and counts");
}
Check(TargetLabels(planner.Accepted) == "Compass / brass, Lantern / paper", "replay reads the same choices");

RoundPlan second = Accept(planner);
RoundPlan third = Accept(planner);
Check(second.Number == 2 && TargetLabels(second) == "Key / iron, Shell / striped", "second round balances families");
Check(third.Number == 3 && TargetLabels(third) == "Kite / linen, Clock / round", "third round balances families");
Check(planner.Families.All(family => planner.TargetUses(family) == 1), "even exposure after three rounds");
RoundPlan fourth = Accept(planner);
Check(fourth.Number == 4 && TargetLabels(fourth) == "Compass / blue, Lantern / glass", "variants rotate");
Check(TargetLabels(first) == "Compass / brass, Lantern / paper", "new rounds leave old snapshots readable");

foreach (RoundPlan plan in new[] { first, second, third, fourth })
{
    Check(plan.Targets.Count == 2 && plan.Distractors.Count == 2 &&
        plan.Targets.Concat(plan.Distractors).Select(item => item.Family).Distinct().Count() == 4,
        "targets and distractors use distinct families");
    RoundPlan next = Accept(replay);
    Check(next.Targets.SequenceEqual(plan.Targets) && next.Distractors.SequenceEqual(plan.Distractors),
        "same catalog and quotas produce the same sequence");
}
Check(planner.TryAccept(6, 0, out _) && planner.Accepted.Targets.Count == 6, "full catalog fits without distractors");

Console.WriteLine("PASS: distinct targets and distractors, quota rejection, replay, family and variant balance, repeatability.");

static RoundPlan Accept(RoundPlanner planner)
{
    Check(planner.TryAccept(2, 2, out string error), error ?? "accept round");
    return planner.Accepted;
}

static string TargetLabels(RoundPlan plan) => string.Join(", ", plan.Targets);

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException($"Round planner check failed: {message}");
}
