using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using static Marvel.Rules.Play.Steps;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Maps scheduled procedures to their triggering conditions.</summary>
public static class StepConditions
{
    private static readonly ReadOnlyDictionary<string, ImmutableArray<string>> Conditions = new(
        new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal)
    {
        [PlaceThreat] = [ThreatWouldBePlaced],
        [PlaceThreatEffect] = [ThreatWouldBePlaced],
        [MainSchemeCompletionOperation.Completed] = [MainSchemeCompletionOperation.Completed],

        // Two conditions at one moment again: an attack *is* an activation
        // (`rr:activation`, "whenever an enemy attacks or schemes, it is
        // considered to have activated"), so both are true of the same
        // occurrence and `rr:triggering-condition.2` gives them one window
        // pair between them.
        [Steps.Attack] = [EnemyActivates, AttackInitiated],
        [Scheme] = [EnemyActivates, EnemySchemes],
        [SchemeThreat] = [ThreatWouldBePlaced],
        [EndSchemeEarly] = [SchemeEnds],
        [GiveBoostCard] = ["WhenBoostCardGiven"],
        [DeclareDefender] = ["WhenDefenderDeclared"],
        [HeroDefenseTiming.Defends] = [HeroDefenseTiming.Defends],
        [BoostFaceupOperation.Faceup] = [BoostFaceupOperation.Faceup],
        [FlipBoostCards] = ["WhenBoostCardsFlipped"],
        // Damage from an attack is imminent before this step applies. Whether
        // it was dealt is known only afterwards: Tough may prevent it, so the
        // applying step adds `DamageDealt` only when damage actually lands.
        // `rr:triggering-condition.2` still gives both one occurrence and one
        // pair of windows.
        [DealAttackDamage] = [DamageWouldBeDealt],
        [PrepareIndirectAttackDamage] = [DamageWouldBeDealt],
        [EndAttack] = [AttackEnds],
        [DealEncounterCards] = ["WhenEncounterCardsDealt"],
        [RevealEncounterCard] = [CardRevealed],
        [TurnAction] = [TurnAction],
        [CardDefeated] = [CardDefeated],
        [CharacterAttacks] = [AttackInitiated],
        [CharacterThwarts] = [CharacterThwartsScheme],
        [DamageWouldBeDealt] = [DamageWouldBeDealt],
        [CardWouldBeDefeated] = [CardWouldBeDefeated],
        // Playing a non-event card is one occurrence that both plays the card
        // and makes it enter play. `rr:triggering-condition.2` gives a single
        // occurrence that creates several triggering conditions one pair of
        // windows rather than one pair per description of the moment.
        [CardPlayed] = [CardPlayed, CardEntersPlay],
        [EventPlayed] = [CardPlayed],
        [EventPlayTiming.Initiated] = [EventPlayTiming.Initiated],
        [CardEntersPlay] = [CardEntersPlay],
        [FormChanged] = [FormChanged],
        [PlayerTurnTiming.Ends] = [PlayerTurnTiming.Ends],
        [PlayerTurnTiming.Begins] = [PlayerTurnTiming.Begins],
        [BasicPowerTiming.RecoveryUsed] = [BasicPowerTiming.RecoveryUsed],
        [ChooseOption] = ["WhenOptionChosen"],
        [PassFirstPlayerToken] = ["WhenFirstPlayerTokenPassed"],
        [VillainStepOneTiming.Completed] = [VillainStepOneTiming.Completed],

        // Two conditions at one moment, because `rr:villain-phase.step.6` is
        // titled "End of Villain Phase and Round" and both are reached there.
        // `rr:triggering-condition.2` gives them one interrupt window and one
        // response window between them.
        [EndVillainPhase] = [PhaseEnd.VillainPhaseEnds, PhaseEnd.RoundEnds],
        [EndPlayerPhase] = [PhaseEnd.PlayerPhaseEnds],
    });

    /// <summary>The triggering conditions a step creates.</summary>
    /// <param name="what">One of the step names here.</param>
    public static IReadOnlyList<string> ConditionsOf(string what) =>
        Conditions.TryGetValue(what, out var conditions) ? conditions : [what];

    /// <summary>
    /// Every triggering condition any step in this engine produces.
    /// </summary>
    /// <remarks>
    /// Derived from the table above rather than listed again, so that it cannot
    /// fall behind it. What it is for: an authored card names the condition it
    /// answers, and a card naming one nothing ever produces would sit in the
    /// dataset looking implemented and never fire. Holding the two sets against
    /// each other turns that into a failing test.
    /// </remarks>
    public static IReadOnlySet<string> EveryCondition { get; } =
        Conditions.Values.SelectMany(each => each).Concat(
                // These depend on the concrete occurrence rather than solely
                // on its scheduled step name.
                [BasicPowerTiming.HeroAttackInitiated, BasicPowerTiming.HeroUseInitiated, DamageDealt, ThreatPlaced, MinionEngagement.Engaged, VillainStepOneTiming.ThreatPlaced, SchemeEnds, BasicAttackEnds, BasicPowerTiming.ThwartEnds,
                    BoostFaceupOperation.AttackFaceup]).ToFrozenSet(StringComparer.Ordinal);
}
