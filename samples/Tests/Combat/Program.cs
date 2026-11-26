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

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
