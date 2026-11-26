using System.Numerics;
using GameplaySamples.LaunchHandoff;

var guide = new LaunchGuide(new Vector2(-1f, 1f), new Vector2(3f, 3f), 0.8f);
Check(guide.PositionAt(-1f) == guide.Start && guide.PositionAt(1f) == guide.End, "endpoints clamp");
Check(Vector2.Distance(guide.PositionAt(0.4f), new Vector2(1f, 2f)) < 0.0001f, "straight guide midpoint");
Check(guide.ReleaseVelocity(true) == new Vector2(5f, 2.5f), "preserved velocity");
Check(guide.ReleaseVelocity(false) == Vector2.Zero, "zero velocity");

foreach (float step in new[] { 0.02f, 0.031f, 0.40f })
{
    float elapsed = 0f;
    while (elapsed < guide.Duration)
        elapsed += step;
    Check(guide.PositionAt(elapsed) == guide.End, "different steps reach the same endpoint");
    Check(guide.ReleaseVelocity(true) == new Vector2(5f, 2.5f), "final step does not distort release velocity");
}

foreach (float duration in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
    Reject(() => new LaunchGuide(Vector2.Zero, Vector2.One, duration), "invalid guide duration");
Reject(() => new LaunchGuide(Vector2.Zero, new Vector2(float.NaN, 0f), 1f), "invalid guide endpoint");
Reject(() => guide.PositionAt(float.NaN), "invalid elapsed time");

Console.WriteLine("PASS: guide endpoints, deterministic release velocity, zero vs preserved release, invalid inputs.");

static void Reject(Action action, string message)
{
    try
    {
        action();
    }
    catch (ArgumentException)
    {
        return;
    }
    throw new InvalidOperationException($"Launch guide accepted {message}.");
}

static void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException($"Launch handoff check failed: {name}");
}
