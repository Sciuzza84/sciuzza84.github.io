using GameplaySamples.ArPlacement;

static void Check(bool condition, string description)
{
    if (!condition)
        throw new InvalidOperationException(description);
    Console.WriteLine("PASS " + description);
}

var gate = new PlacementGate();
gate.Update(0.85f, 15f, 1f);
Check(gate.Progress > 0f && !gate.TryPlace(), "a partial interval cannot confirm a target");
gate.Update(0.69f, 15f, 0.1f);
Check(gate.Progress == 0f, "dropping image quality clears accumulated time");
gate.Update(0.85f, 15f, 1f);
gate.Update(0.85f, -31f, 0.1f);
Check(gate.Progress == 0f, "tilt outside either bound clears accumulated time");
gate.Update(0.70f, -30f, 0.75f);
gate.Update(0.70f, 30f, 0.75f);
Check(gate.IsReady && gate.Progress == 1f, "quality and tilt thresholds are inclusive");
gate.Update(0.69f, 15f, 0f);
Check(!gate.IsReady && !gate.TryPlace(), "signal loss after readiness still gates confirmation");
gate.Update(0.90f, 0f, 5f);
Check(gate.Progress == 1f && gate.TryPlace() && !gate.TryPlace(), "only one confirmation succeeds");
gate.Update(0f, 90f, 1f);
Check(gate.IsPlaced, "confirmed anchor remains placed");
gate.Reset();
Check(!gate.IsPlaced && gate.Progress == 0f, "reset starts a new session");

var pair = AnchorLayout.Centers(2.4f);
Check(pair.Left < 0f && pair.Right > 0f &&
      Math.Abs(pair.Left + pair.Right) < 0.001f &&
      Math.Abs(pair.Right - pair.Left - 2.4f) < 0.001f,
    "the pair remains centered on the anchor and preserves the specified separation");
