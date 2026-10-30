using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Retains the containing ability until the character power's response window ends.</summary>
internal sealed class AbilityPowerAftermath(AbilityResolutionState initial, string power)
{
    private bool scheduled;

    internal bool Complete(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        if (power is not (BasicPowers.AttackVerb or BasicPowers.ThwartVerb) || scheduled) return true;
        var state = initial.PowerExecution
            ?? throw new InvalidOperationException("The character power has no retained execution");
        // rr:initiating-abilities.2: a response "may be initiated immediately after
        // that triggering condition becomes true." Resolve that power's response
        // window before continuing the containing ability's following effects.
        // rr:event: "then place the event card in its owner's discard pile."
        // Structural selection wrappers do not leave another effect to resolve.
        var consequential = Consequential(state);
        if (consequential is null && state.Decoded.Frames.All(frame => frame is ChoiceFrame or ChoiceOtherwiseFrame
            or ConditionalFrame or DefenseFrame
            || frame is SequenceFrame sequence && sequence.Next >= sequence.Count)) return true;
        var owner = state.WindowOwner
            ?? throw new InvalidOperationException("The character power has no response-window owner");
        var occurrence = state.Occurrence
            ?? throw new InvalidOperationException("The character power has no response occurrence");
        scheduled = true;
        var capture = execution.Capture(cast, state.Decoded.Ordinal) with
        { Reason = AbilityResumeReason.PowerAftermath };
        var continuation = AbilityContinuations.Step(capture,
            AbilityContinuationStage.Resume, owner.Round, plan: true);
        cast.World.Agenda.AfterResponseWindow(cast.Execution!, owner, occurrence, continuation, consequential);
        cast.Suspend();
        cast.CompleteResolution();
        return false;
    }

    private static PhaseStep? Consequential(AbilityPowerExecutionState state)
    {
        // rr:ally.3: "After an ally is used to attack or thwart, deal consequential damage".
        // rr:consequential-damage.1: this is "after resolving abilities that are triggered
        // by the ally attacking or thwarting". Only card-labelled powers reach this
        // interpreter aftermath; basic powers own their scheduled damage separately.
        if (state.Occurrence?.ActorFacts is not { IsAlly: true } actor) return null;
        bool attack = state.Kind == BasicPowers.AttackVerb;
        return new(new ConsequentialDamageOperation(attack
            ? ConsequentialDamageKind.AllyConsequentialDamage
            : ConsequentialDamageKind.AllyThwartConsequentialDamage,
            state.Occurrence.Target)
            { PerformerIncarnation = actor.Incarnation, UsesAttack = state.ThwartActorUsingAttack is not null },
            state.WindowOwner!.Value.Round, 9, Subject: actor.Card, Seat: actor.Controller);
    }
}
