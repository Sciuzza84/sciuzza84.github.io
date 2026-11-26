using System;
using System.Numerics;

namespace GameplaySamples.LaunchHandoff
{
    public readonly struct LaunchGuide
    {
        public LaunchGuide(Vector2 start, Vector2 end, float duration)
        {
            if (!IsFinite(duration) || duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration), "The guide duration must be finite and positive.");
            if (!IsFinite(start.X) || !IsFinite(start.Y))
                throw new ArgumentOutOfRangeException(nameof(start), "The start position must be finite.");
            if (!IsFinite(end.X) || !IsFinite(end.Y))
                throw new ArgumentOutOfRangeException(nameof(end), "The end position must be finite.");

            Vector2 velocity = (end - start) / duration;
            if (!IsFinite(velocity.X) || !IsFinite(velocity.Y))
                throw new ArgumentException("The guide must produce a finite velocity.");

            Start = start;
            End = end;
            Duration = duration;
            Velocity = velocity;
        }

        public Vector2 Start { get; }
        public Vector2 End { get; }
        public float Duration { get; }
        public Vector2 Velocity { get; }

        public Vector2 PositionAt(float elapsed)
        {
            if (!IsFinite(elapsed))
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            if (elapsed <= 0f)
                return Start;
            if (elapsed >= Duration)
                return End;
            return Start + Velocity * elapsed;
        }

        public Vector2 ReleaseVelocity(bool preserve) => preserve ? Velocity : Vector2.Zero;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
