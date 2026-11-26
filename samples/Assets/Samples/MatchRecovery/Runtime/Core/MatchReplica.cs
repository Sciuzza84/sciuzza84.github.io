namespace GameplaySamples.MatchRecovery
{
    public enum Team
    {
        A,
        B
    }

    public enum UpdateResult
    {
        Applied,
        AlreadySeen,
        SnapshotRequired,
        Invalid
    }

    public enum SnapshotResult
    {
        Applied,
        AlreadyCurrent,
        TooOld,
        Conflict,
        Invalid
    }

    public readonly struct ScoreUpdate
    {
        public ScoreUpdate(int revision, Team team, int points)
        {
            Revision = revision;
            Team = team;
            Points = points;
        }

        public int Revision { get; }
        public Team Team { get; }
        public int Points { get; }
    }

    public readonly struct MatchSnapshot
    {
        public MatchSnapshot(int revision, int scoreA, int scoreB)
        {
            Revision = revision;
            ScoreA = scoreA;
            ScoreB = scoreB;
        }

        public int Revision { get; }
        public int ScoreA { get; }
        public int ScoreB { get; }
    }

    public sealed class MatchReplica
    {
        public MatchSnapshot State { get; private set; }
        public int MinimumSnapshotRevision { get; private set; }

        public UpdateResult Apply(ScoreUpdate update)
        {
            if (update.Revision < 1 || update.Points < 1 || (update.Team != Team.A && update.Team != Team.B))
                return UpdateResult.Invalid;
            if (update.Revision <= State.Revision)
                return UpdateResult.AlreadySeen;

            // Keep the last complete state until the authority covers all observed revisions.
            // Delayed events can enter in any order, so this replica does not buffer a partial replay.
            if (MinimumSnapshotRevision > 0 || update.Revision - State.Revision != 1)
            {
                if (update.Revision > MinimumSnapshotRevision)
                    MinimumSnapshotRevision = update.Revision;
                return UpdateResult.SnapshotRequired;
            }

            int scoreA = State.ScoreA;
            int scoreB = State.ScoreB;
            int score = update.Team == Team.A ? scoreA : scoreB;
            if (update.Points > int.MaxValue - score)
                return UpdateResult.Invalid;

            if (update.Team == Team.A)
                scoreA += update.Points;
            else
                scoreB += update.Points;
            State = new MatchSnapshot(update.Revision, scoreA, scoreB);
            return UpdateResult.Applied;
        }

        public SnapshotResult Install(MatchSnapshot snapshot)
        {
            if (snapshot.Revision < 0 || snapshot.ScoreA < 0 || snapshot.ScoreB < 0)
                return SnapshotResult.Invalid;
            if (snapshot.Revision < State.Revision || snapshot.Revision < MinimumSnapshotRevision)
                return SnapshotResult.TooOld;
            if (snapshot.Revision == State.Revision)
                return snapshot.ScoreA == State.ScoreA && snapshot.ScoreB == State.ScoreB
                    ? SnapshotResult.AlreadyCurrent
                    : SnapshotResult.Conflict;

            State = snapshot;
            MinimumSnapshotRevision = 0;
            return SnapshotResult.Applied;
        }
    }
}
