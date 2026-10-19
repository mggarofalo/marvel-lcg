using static Marvel.Cards.Run.AbilityPowerSuspension;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Resumes one card-labelled power and then its containing ability.</summary>
internal static class AbilityPowerResolution
{
    internal static void ResolveCardAttack(this AbilityResolutionExecution execution,
        World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events)
    {
        var continuation = RequireContinuation(attack.Continuation, BasicPowers.AttackVerb);
        var cast = CreatePowerState(
            world, attack.Source, attack.Player, BasicPowers.AttackVerb,
            continuation, attack.Origin ?? occurrence, events) with
        {
            PowerAmount = attack.Amount,
            EventTrigger = attack.Trigger,
            AbilityActor = world.Cards[attack.Attacker],
            PowerActor = occurrence.Actor >= 0 ? world.Cards[occurrence.Actor] : null,
        };
        if (attack.SurgeGained) cast.GainedKeywords.Add("surge");
        cast.Choose(world.Cards[attack.Enemy]);
        execution.ResolvePower(cast, continuation);
    }

    internal static void ResolveCardThwart(this AbilityResolutionExecution execution,
        World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events)
    {
        var continuation = RequireContinuation(thwart.Continuation, BasicPowers.ThwartVerb);
        var cast = CreatePowerState(
            world, thwart.Source, thwart.Player, BasicPowers.ThwartVerb,
            continuation, thwart.Origin ?? occurrence, events) with
        {
            PowerAmount = thwart.Amount,
            ThwartActorUsingAttack = thwart.UsesAttack ? thwart.Thwarter : null,
            ImminentThreat = thwart.ImminentThreat,
            EventTrigger = thwart.Trigger,
            AbilityActor = world.Cards[thwart.Thwarter],
            PowerActor = occurrence.Actor >= 0 ? world.Cards[occurrence.Actor] : null,
        };
        if (thwart.SurgeGained) cast.GainedKeywords.Add("surge");
        cast.Choose(world.Cards[thwart.Scheme]);
        execution.ResolvePower(cast, continuation);
    }

    private static AbilityPowerContinuation RequireContinuation(CardPowerContinuation? continuation, string power) =>
        continuation as AbilityPowerContinuation ?? throw new RulesNotImplementedException(
            $"card {power.ToLowerInvariant()} has no interpreter continuation");

    private static AbilityResolutionState CreatePowerState(
        World world, int sourceId, int player, string power,
        AbilityPowerContinuation continuation, Occurrence occurrence, List<GameEvent> events)
    {
        if (sourceId < 0 || sourceId >= world.Cards.Count)
            throw new RulesNotImplementedException(
                $"card {power.ToLowerInvariant()} has no reconstructable source");

        return new(world, world.Cards[sourceId], occurrence, player, events)
        {
            FinalStep = continuation.Capture.Cursor.FinalStep,
            Power = power,
            PowerTargets = [.. continuation.Targets.Select(id => world.Cards[id])],
            EachPlayerFrame = continuation.Capture.Cursor.EachPlayerFrame,
            FinalPlayer = continuation.Capture.Cursor.FinalPlayer,
            AbilityPlayer = continuation.Capture.Invocation.AbilityPlayer,
        };
    }

    private static void ResolvePower(
        this AbilityResolutionExecution execution, AbilityResolutionState cast, AbilityPowerContinuation continuation)
    {
        var world = cast.World;
        var source = cast.Source;
        string power = cast.Power!;
        int player = cast.Player;
        var restored = AbilityContinuations.DecodePower(
            execution.program, source, continuation, power);
        cast = cast with { Tier = restored.Ability.Trigger.Timing };
        execution.RestorePersisted(cast, continuation.Capture);
        if (execution.SuspendsPowerEffect(restored.Body, cast))
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' suspends inside a {power.ToLowerInvariant()}, which is not implemented");

        cast.RestoreAbility(restored.Ordinal, restored.Frames.Add(new PowerFrame()), continuation.Capture.Invocation.Address.Face);
        execution.RestoreAlteredFromFrames(cast, restored.Frames);
        cast.TrackResolution(restored.Ordinal);
        var attackModifiers = power == BasicPowers.AttackVerb
            ? execution.EventModifierEffects(cast, "attackDamage")
            : [];
        cast.PowerExecution = new(restored, continuation, power, cast.PowerActor?.ObjectId,
            cast.PowerAmount, [.. attackModifiers]) { ThwartActorUsingAttack = cast.ThwartActorUsingAttack };
        execution.Run(restored.Body, cast);
        if (cast.Suspended)
        {
            cast.CompleteResolution();
            return;
        }
        execution.FinishResumedPower(cast);
    }

    internal static void FinishResumedPower(this AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        var state = cast.PowerExecution ?? throw new InvalidOperationException("The power has no execution state.");
        cast.PowerExecution = null;
        cast.ThwartActorUsingAttack = null;
        // One attack modifier applies to every damage node and is consumed only after its body completes.
        foreach (var modifier in state.Modifiers) cast.World.Effects.Use(modifier);
        FinishAttack(cast, cast.Player, state.Kind);
        RestoreOuterChosen(state.Continuation.Capture.Memory.Chosen, cast);
        cast.RestoreAbility(state.Decoded.Ordinal, state.Decoded.Frames,
            state.Continuation.Capture.Invocation.Address.Face);
        execution.CompletePower(state.Decoded, cast with { Power = null, PowerActor = null, PowerTargets = [], PowerAmount = -1 });
    }

    private static void FinishAttack(AbilityResolutionState cast, int player, string power)
    {
        if (power != BasicPowers.AttackVerb) return;
        var world = cast.World;
        var attacker = cast.PowerActor ?? world.Seats[player].IdentityCard;
        if (AttackKeywords.Has(world, attacker, cast.Source, Keywords.Ranged)) return;
        foreach (var target in cast.Attacked.DistinctBy(card => card.ObjectId))
            DamageAttacks.Retaliate(world, world.Facts, target, attacker, cast.Trigger, cast.Events);
    }

    private static void RestoreOuterChosen(
        AbilityContinuationCardBinding? binding, AbilityResolutionState cast)
    {
        if (binding is not { } chosen) return;
        cast.RestorePersistedSelection(cast.World.Cards[chosen.ObjectId], chosen.AreaId,
            chosen.Incarnation, overwriteChosen: true);
    }

    private static void CompletePower(
        this AbilityResolutionExecution execution, DecodedPowerContinuation restored, AbilityResolutionState cast)
    {
        var next = AbilityContinuations.AfterPower(
            restored, execution.Capture(cast, restored.Ordinal), cast.Suspended);
        if (next is not null)
        {
            _ = execution.ResumeContinuation(cast, cast.Source, next);
            return;
        }
        execution.DiscardEvent(cast.Source, cast);
        cast.CompleteResolution();
    }
}
