using GameplaySamples.Combat;
using Move = GameplaySamples.Combat.OpponentRules.Action;
using Availability = GameplaySamples.Combat.OpponentRules.Availability;
using Step = GameplaySamples.Combat.EndPhaseRules.Step;

ExpectEndOfPhaseToDecideWhetherEnemyResponds();
ExpectCardToRespectEnergyAndRotateDeck();
ExpectRecoveryToUnlockAnActionOnNextTurn();
ExpectCappedStatusAndUnaffordableAttackToLeadToSwapOrStop();
ExpectAffordableStatusToRemainAnOption();
ExpectRecoveryToStayWithinManaCapacity();
Console.WriteLine("PASS: 6 combat scenarios — phase order, card cost, recovery, reserve, caps and limits.");
ExpectFeedbackBurstToStayBoundedAndReuseItsSlots();
ExpectFeedbackToRespectKindRadiusAndNearestAnchor();
ExpectMergingToKeepTheOriginalExpiry();
Console.WriteLine("PASS: floating feedback burst, capacity, reuse, expiry, kind isolation and nearest-anchor merging.");

static void ExpectEndOfPhaseToDecideWhetherEnemyResponds()
{
    EndPhaseRules.Result defeat = EndPhaseRules.Resolve(2, 2, false);
    Check(defeat.Health == 0 &&
        defeat.Steps.SequenceEqual(new[] { Step.StatusApplied, Step.PlayerDefeated }),
        "A lethal effect must prevent any enemy response.");

    EndPhaseRules.Result survive = EndPhaseRules.Resolve(3, 2, false);
    Check(survive.Health == 1 && survive.Steps.SequenceEqual(new[] { Step.StatusApplied, Step.EnemyResponded }),
        "A nonlethal effect must precede exactly one enemy response.");
}

static void ExpectCardToRespectEnergyAndRotateDeck()
{
    EndPhaseRules.Result blocked = EndPhaseRules.Resolve(3, 1, true);
    Check(blocked.Health == 3 && blocked.Energy == 1 &&
        blocked.Deck.SequenceEqual(new[] { "Ward", "Feint", "Torch" }) &&
        blocked.Steps.SequenceEqual(new[] { Step.CardRejected }),
        "An unaffordable card must leave the phase, energy and deck intact.");

    EndPhaseRules.Result played = EndPhaseRules.Resolve(3, 2, true);
    Check(played.Energy == 0 && played.Deck.SequenceEqual(new[] { "Feint", "Torch", "Ward" }) &&
        played.Steps.SequenceEqual(new[]
        {
            Step.CardPlayed, Step.CardReturnedToDeck, Step.StatusApplied, Step.EnemyResponded
        }),
        "A legal play pays once, rotates the deck and finishes the phase in order.");
}

static void ExpectRecoveryToUnlockAnActionOnNextTurn()
{
    OpponentRules.Decision waiting = OpponentRules.Choose(1, 4, 2, true, 0);
    Check(waiting.Chosen == Move.Wait && waiting.NextMana == 3 && waiting.Attack == Availability.AfterRecovery,
        "The opponent may wait when it can attack after the next recovery.");
    OpponentRules.Decision recovered = OpponentRules.Choose(waiting.NextMana, 4, 2, true, 0);
    Check(recovered.Chosen == Move.Attack && recovered.Attack == Availability.Ready,
        "The opponent must attack once the required mana is available.");
}

static void ExpectCappedStatusAndUnaffordableAttackToLeadToSwapOrStop()
{
    OpponentRules.Decision swap = OpponentRules.Choose(1, 2, 1, true, 1);
    Check(swap.Chosen == Move.Swap && swap.CanSwap && swap.Attack == Availability.BeyondCapacity &&
        swap.Status == Availability.AlreadyAtLimit,
        "With capped status and an impossible attack, the opponent must switch to the living reserve.");
    OpponentRules.Decision stuck = OpponentRules.Choose(1, 2, 1, true, 0);
    Check(stuck.Chosen == Move.None && !stuck.CanSwap,
        "A defeated or absent reserve must not lead to an invalid swap or an endless wait.");
}

static void ExpectAffordableStatusToRemainAnOption()
{
    OpponentRules.Decision improve = OpponentRules.Choose(2, 2, 1, false, 0);
    Check(improve.Chosen == Move.ImproveStatus && improve.Status == Availability.Ready,
        "An uncapped, affordable status move must be chosen when attack is unavailable.");
}

static void ExpectRecoveryToStayWithinManaCapacity()
{
    OpponentRules.Decision capped = OpponentRules.Choose(1, 4, int.MaxValue, true, 0);
    Check(capped.NextMana == 4, "Recovery must cap mana without overflowing.");
    OpponentRules.Decision tooSoon = OpponentRules.Choose(0, 4, 1, true, 0);
    Check(tooSoon.Chosen == Move.None && tooSoon.Attack == Availability.NeedsMoreMana,
        "The sample looks ahead by a single turn.");
}

static void ExpectFeedbackBurstToStayBoundedAndReuseItsSlots()
{
    FeedbackPool pool = FeedbackBurst.CreatePool();
    FeedbackBurst.Send(pool);
    Check(pool.Capacity == 4 && pool.Started == 4 && pool.Merged == 3 && pool.Reused == 0 && pool.Dropped == 1,
        "The first eight events must fit four slots, merge three and drop only the unmatched fifth label.");
    Check(pool.TryGetActive(0, out FeedbackLabel first) && first.Kind == FeedbackKind.Damage && first.Amount == 12 &&
        pool.TryGetActive(1, out FeedbackLabel second) && second.Kind == FeedbackKind.Heal && second.Amount == 7 &&
        pool.TryGetActive(2, out FeedbackLabel third) && third.Amount == 10 &&
        pool.TryGetActive(3, out FeedbackLabel fourth) && fourth.Amount == 9,
        "Damage and healing on the same point must each produce the expected, separate total.");

    for (int tick = 0; tick < pool.LifetimeTicks; tick++)
        pool.Advance();
    Check(Enumerable.Range(0, pool.Capacity).All(index => !pool.TryGetActive(index, out _)),
        "The first burst's four slots must expire together.");
    FeedbackBurst.Send(pool);
    Check(pool.Started == 4 && pool.Reused == 4 && pool.Merged == 6 && pool.Dropped == 2 &&
        pool.TryGetActive(0, out FeedbackLabel replacement) && replacement.Amount == first.Amount &&
        replacement.ExpiresAt > first.ExpiresAt,
        "The second burst must reuse all four indexed slots without carrying their prior totals.");
}

static void ExpectFeedbackToRespectKindRadiusAndNearestAnchor()
{
    var pool = new FeedbackPool(capacity: 3, lifetimeTicks: 3, mergeRadius: 5);
    Check(pool.Emit(FeedbackKind.Damage, 0, 0, 4) == FeedbackOutcome.Started &&
        pool.Emit(FeedbackKind.Damage, 8, 0, 6) == FeedbackOutcome.Started &&
        pool.Emit(FeedbackKind.Heal, 0, 0, 2) == FeedbackOutcome.Started,
        "Out-of-range events and other kinds need distinct slots.");
    Check(pool.Emit(FeedbackKind.Damage, 5, 0, 7) == FeedbackOutcome.Merged &&
        pool.TryGetActive(0, out FeedbackLabel first) && first.Amount == 4 &&
        pool.TryGetActive(1, out FeedbackLabel nearest) && nearest.Amount == 13 && nearest.X == 8,
        "A full pool should merge into the nearest same-kind anchor and leave its position intact.");

    Check(pool.Emit(FeedbackKind.Heal, 3, 4, 5) == FeedbackOutcome.Merged &&
        pool.TryGetActive(2, out FeedbackLabel heal) && heal.Amount == 7 && heal.X == 0 && heal.Y == 0,
        "The radius includes its boundary, even on a diagonal.");
    Check(pool.Emit(FeedbackKind.Heal, 4, 4, 9) == FeedbackOutcome.Dropped &&
        pool.Emit(FeedbackKind.Damage, int.MinValue, int.MaxValue, 2) == FeedbackOutcome.Dropped,
        "An unmatched event cannot grab a fourth slot or overflow the coordinate-distance check.");
}

static void ExpectMergingToKeepTheOriginalExpiry()
{
    var pool = new FeedbackPool(capacity: 1, lifetimeTicks: 2, mergeRadius: 1);
    pool.Emit(FeedbackKind.Damage, 100, 100, int.MaxValue);
    pool.Advance();
    Check(pool.Emit(FeedbackKind.Damage, 101, 100, 2) == FeedbackOutcome.Merged &&
        pool.TryGetActive(0, out FeedbackLabel merged) && merged.Amount == (long)int.MaxValue + 2 && merged.ExpiresAt == 2,
        "The total may exceed int range, but merging must preserve the initial expiry.");
    pool.Advance();
    Check(!pool.TryGetActive(0, out _) && pool.Emit(FeedbackKind.Heal, 100, 100, 2) == FeedbackOutcome.Reused &&
        pool.TryGetActive(0, out FeedbackLabel reused) && reused.Kind == FeedbackKind.Heal && reused.Amount == 2,
        "The slot must expire at the deadline, reset its amount and accept another kind on reuse.");
}

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
