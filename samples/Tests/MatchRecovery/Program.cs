using GameplaySamples.MatchRecovery;

var client = new MatchReplica();
var first = new ScoreUpdate(1, Team.A, 2);
Check(client.Apply(first) == UpdateResult.Applied, "The first event must advance the replica.");
CheckState(client, 1, 2, 0);
Check(client.Apply(first) == UpdateResult.AlreadySeen, "An event already applied must be idempotent.");
CheckState(client, 1, 2, 0);

var third = new ScoreUpdate(3, Team.A, 4);
Check(client.Apply(third) == UpdateResult.SnapshotRequired, "A gap must request a snapshot.");
Check(client.MinimumSnapshotRevision == 3, "Recovery must cover the observed revision.");
CheckState(client, 1, 2, 0);
Check(client.Apply(new ScoreUpdate(2, Team.B, 3)) == UpdateResult.SnapshotRequired,
    "A delayed event must not partially recover the replica.");
Check(client.MinimumSnapshotRevision == 3, "A delayed event cannot lower the recovery bound.");
Check(client.Install(new MatchSnapshot(2, 2, 3)) == SnapshotResult.TooOld, "An earlier snapshot is insufficient.");
CheckState(client, 1, 2, 0);

Check(client.Apply(new ScoreUpdate(4, Team.B, 2)) == UpdateResult.SnapshotRequired,
    "A later delivery while recovering must raise the bound.");
Check(client.Install(new MatchSnapshot(3, 6, 3)) == SnapshotResult.TooOld, "A snapshot for the earlier gap is now stale.");
Check(client.Install(new MatchSnapshot(4, 6, 5)) == SnapshotResult.Applied, "A covering snapshot must recover the replica.");
Check(client.MinimumSnapshotRevision == 0, "Recovery clears the bound.");
CheckState(client, 4, 6, 5);
Check(client.Apply(third) == UpdateResult.AlreadySeen, "A replay before the recovered revision is idempotent.");
Check(client.Install(new MatchSnapshot(4, 6, 5)) == SnapshotResult.AlreadyCurrent, "Repeated snapshot is idempotent.");
Check(client.Install(new MatchSnapshot(4, 7, 5)) == SnapshotResult.Conflict, "Conflicting content at the same revision is detected.");
Check(client.Install(new MatchSnapshot(3, 6, 3)) == SnapshotResult.TooOld, "A late snapshot cannot regress the replica.");
CheckState(client, 4, 6, 5);
Check(client.Apply(new ScoreUpdate(5, Team.A, 1)) == UpdateResult.Applied, "Normal delivery resumes after recovery.");
CheckState(client, 5, 7, 5);

Check(client.Apply(new ScoreUpdate(6, (Team)2, 1)) == UpdateResult.Invalid, "Unknown teams are rejected.");
Check(client.Apply(new ScoreUpdate(6, Team.A, -1)) == UpdateResult.Invalid, "Negative deltas are rejected.");
Check(client.Install(new MatchSnapshot(10, -1, 0)) == SnapshotResult.Invalid, "Invalid snapshots are rejected.");
CheckState(client, 5, 7, 5);
Check(client.Install(new MatchSnapshot(6, int.MaxValue, 5)) == SnapshotResult.Applied, "A valid snapshot may hold maximum score.");
Check(client.Apply(new ScoreUpdate(7, Team.A, 1)) == UpdateResult.Invalid, "Overflow cannot produce a negative score.");
CheckState(client, 6, int.MaxValue, 5);

Console.WriteLine("Match Recovery PASS: gaps, bounded recovery, stale snapshots, replay, conflicting state, resumption and invalid inputs.");

static void CheckState(MatchReplica replica, int revision, int scoreA, int scoreB)
{
    MatchSnapshot state = replica.State;
    Check(state.Revision == revision && state.ScoreA == scoreA && state.ScoreB == scoreB,
        $"Expected revision {revision}, score {scoreA}:{scoreB}. Got {state.Revision}, {state.ScoreA}:{state.ScoreB}.");
}

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
