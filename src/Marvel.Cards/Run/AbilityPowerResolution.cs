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
        execution.ResolvePower(cast, continuation, occurrence);
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
        execution.ResolvePower(cast, continuation, occurrence);
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
            PowerTargets = [.. CurrentTargets(world, continuation)],
            EachPlayerFrame = continuation.Capture.Cursor.EachPlayerFrame,
            FinalPlayer = continuation.Capture.Cursor.FinalPlayer,
            AbilityPlayer = continuation.Capture.Invocation.AbilityPlayer,
        };
    }

    private static IEnumerable<Card> CurrentTargets(World world, AbilityPowerContinuation continuation)
    {
        if (continuation.SelectedTargets.IsDefault)
            return continuation.Targets.Select(id => world.Cards[id]);
        // rr:leaves-play.1: a departing card is a new copy. The declared
        // recipient facts survive, but damage cannot follow the new copy.
        return continuation.SelectedTargets.Select(binding => (Binding: binding, Card: world.Cards[binding.ObjectId]))
            .Where(item => item.Card.Incarnation == item.Binding.Incarnation
                && DeckTypes.IsInPlay(item.Card.Area.Type))
            .Select(item => item.Card);
    }

    private static void ResolvePower(
        this AbilityResolutionExecution execution, AbilityResolutionState cast, AbilityPowerContinuation continuation, Occurrence occurrence)
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
            cast.PowerAmount, [.. attackModifiers]) { ThwartActorUsingAttack = cast.ThwartActorUsingAttack, Occurrence = occurrence, WindowOwner = world.Agenda.Current };
        if (power == BasicPowers.AttackVerb)
        {
            if (Marvel.Cards.Dsl.AbilityStagedAttackLowering.IsBody(restored.Body))
                cast.PowerExecution = cast.PowerExecution with
                { FirstEnemyIncarnation = cast.Chosen!.Incarnation,
                    ExcludedEnemyStages = AbilityStagedAttack.CaptureIdentity(world,
                    cast.Chosen ?? throw new InvalidOperationException("A staged attack requires its first enemy.")) };
            var attacker = cast.PowerActor ?? world.Seats[player].IdentityCard;
            occurrence.Retaliation = new(attacker.ObjectId, attacker.Incarnation,
                AttackKeywords.Has(world, attacker, source, Keywords.Ranged),
                continuation.DeclaredRecipients.IsDefault
                    ? cast.PowerTargets.Select(target => new RetaliationRecipient(
                        Attacked.Capture(target, world.Facts), target.Area.Id))
                    : continuation.DeclaredRecipients.Select(recipient => new RetaliationRecipient(recipient, recipient.Area)),
                cast.Trigger);
        }
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
        if (state.Aftermath is null)
        {
            // One modifier applies throughout the body and is consumed once before aftermath.
            foreach (var modifier in state.Modifiers) cast.World.Effects.Use(modifier);
            state = state with { Aftermath = new AbilityPowerAftermath(cast, state.Kind) };
            cast.PowerExecution = state;
        }
        if (!state.Aftermath.Complete(execution, cast)) return;
        cast.PowerExecution = null;
        cast.ThwartActorUsingAttack = null;
        RestoreOuterChosen(state.Continuation.Capture.Memory.Chosen, cast);
        cast.RestoreAbility(state.Decoded.Ordinal, state.Decoded.Frames,
            state.Continuation.Capture.Invocation.Address.Face);
        cast.Power = null;
        cast.PowerActor = null;
        cast.PowerTargets = [];
        cast.PowerAmount = -1;
        execution.CompletePower(state.Decoded, cast);
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
