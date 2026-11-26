using System;

namespace GameplaySamples.ArPlacement
{
    public sealed class PlacementGate
    {
        public const float MinimumQuality = 0.70f;
        public const float MaximumTilt = 30f;
        public const float RequiredSeconds = 1.5f;

        private float stableSeconds;

        public float Progress => stableSeconds / RequiredSeconds;
        public bool IsReady => !IsPlaced && stableSeconds >= RequiredSeconds;
        public bool IsPlaced { get; private set; }

        public void Update(float quality, float tiltDegrees, float elapsedSeconds)
        {
            if (IsPlaced)
                return;

            // The whole interval must be usable. Pausing the timer here would let
            // a sequence of disconnected good frames count as stable tracking.
            if (!(quality >= MinimumQuality && Math.Abs(tiltDegrees) <= MaximumTilt))
                stableSeconds = 0f;
            else if (elapsedSeconds > 0f)
                stableSeconds = Math.Min(RequiredSeconds, stableSeconds + elapsedSeconds);
        }

        public bool TryPlace()
        {
            if (!IsReady)
                return false;

            IsPlaced = true;
            return true;
        }

        public void Reset()
        {
            stableSeconds = 0f;
            IsPlaced = false;
        }
    }

    public static class AnchorLayout
    {
        // Distance is specified between the objects' origins. Changing scale
        // must preserve those origins in the local space of the same anchor.
        public static (float Left, float Right) Centers(float separation)
        {
            float halfSeparation = separation * 0.5f;
            return (-halfSeparation, halfSeparation);
        }
    }
}
