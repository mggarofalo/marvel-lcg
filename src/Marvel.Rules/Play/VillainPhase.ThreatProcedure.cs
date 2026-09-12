using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>A read-only projection of a forced would-be-defeated interrupt.</summary>

internal static class ThreatProcedure
{
    internal static Prompt? Apply(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        switch (step.What)
        {
            case Steps.PlaceThreat:
                PlaceThreat(world, facts, world.ThreatAbilities, events);
                break;
            case Steps.PlaceThreatEffect:
                ApplyThreat(world, facts, world.ThreatAbilities, events);
                break;
            case Steps.Scheme:
                Scheme(world, facts, step, events);
                break;
            case Steps.SchemeThreat:
                SchemeThreat(world, facts, world.ThreatAbilities, step, events);
                break;
            case Steps.EndSchemeEarly:
                EndSchemeEarly(world, events);
                break;
            default:
                throw new RulesNotImplementedException(
                    $"the threat procedure has no step '{step.What}'");
        }
        return null;
    }

    /// <summary>Step 1. Threat from the main scheme's acceleration field.</summary>
    /// <remarks>
    /// <c>rr:villain-phase.1</c>: "Place the amount of threat indicated in the
    /// main scheme's acceleration field onto that scheme." The engine's name for
    /// that field is <c>EscalationThreat</c>, and it is per-player —
    /// <c>1*</c> on <c>01097b</c>, so one threat at one player and three at
    /// three. Acceleration icons and tokens add more; nothing on the milestone
    /// board has one.
    /// </remarks>
    internal static void PlaceThreat(
        World world, ICardFacts facts, IThreatCardAbilities abilities, List<GameEvent> events)
    {
        if (world.Agenda.Occurrence is { } occurrence)
        {
            long placed = Threat.Apply(world, facts, abilities, occurrence, events);
            // Assault on NORAD says "After placing threat here during step
            // one". A fully prevented assignment did not place threat, so it
            // does not create that response condition (FAQ 01138).
            if (placed > 0)
            {
                occurrence.Also(Steps.VillainPhaseStepOneEnds);
            }
        }
    }

    internal static void ApplyThreat(
        World world, ICardFacts facts, IThreatCardAbilities abilities, List<GameEvent> events)
    {
        var step = world.Agenda.Current
            ?? throw new InvalidOperationException("a threat step has no agenda item");
        var occurrence = world.Agenda.Occurrence
            ?? throw new InvalidOperationException("a threat step has no occurrence");
        long placed = Threat.Apply(world, facts, abilities, occurrence, events);
        if (step.AbilityOccurrence is { } abilityOccurrence
            && step.Tier is { } tier
            && step.AbilityOrdinal >= 0
            && step.Placement is { Source: >= 0 } placement)
        {
            var ability = new PendingAbility(
                placement.Source, tier, step.Seat, step.AbilityOrdinal);
            if (placed > 0)
            {
                abilityOccurrence.Resolve(ability);
            }
            abilityOccurrence.Complete(ability);
        }
    }

    /// <summary>An enemy schemes. <c>rr:scheme-enemy-activation</c>.</summary>
    /// <remarks>
    /// Three steps: give it one facedown boost card from the encounter deck,
    /// resolve that card (flip, add its boost icons to SCH, discard), then place
    /// threat equal to the modified SCH on the main scheme.
    /// </remarks>
    internal static void Scheme(
        World world, ICardFacts facts, PhaseStep step, List<GameEvent> events)
    {
        Card villain = world.Cards[step.Subject];
        int seat = step.Seat;
        // `rr:activation.6`: "if an activating minion leaves play, that
        // minion's activation ends immediately and no further steps of that
        // activation resolve." A scheme is an activation -- `rr:activation`
        // says so -- and a minion can be defeated between being scheduled to
        // scheme and getting to. `rr:in-play-and-out-of-play.2` is what in play
        // means for an encounter card.
        if (!DeckTypes.IsInPlay(villain.Area.Type))
        {
            return;
        }

        // `rr:confuse-confused.1`: "when this character would scheme or thwart,
        // remove each confused status card from it instead." The scheme does
        // not happen, so no boost card is given and no threat is placed.
        if (BasicPowerStatus.Cancelled(world, facts, villain, Statuses.Confused, events))
        {
            return;
        }

        // `rr:activation` -- the other kind, and the one that had no value on
        // the board until now. Set after `rr:stun-stunned`'s cancellation
        // above, because a cancelled activation is not one.
        world.Activation = new EnemyActivation(
            villain.ObjectId, seat, Attacking: false, Id: world.Agenda.Current?.ActivationId ?? -1);

        // **A scheming enemy holds boost cards, plural.**
        // `rr:scheme-enemy-activation.step.1` gives the card to the enemy --
        // "give **it** one facedown boost card" -- and step 2 resolves "each of
        // the scheming enemy's boost cards, one at a time and in the order in
        // which they were dealt", ending at `.step.2.e`: "if the enemy has any
        // boost cards remaining, repeat these steps with the next boost card."
        // That sentence cannot be true of a card drawn and discarded inside one
        // call, which is what this was: exactly one, with nowhere to put a
        // second. the original investigation.
        //
        // So the card goes where the rule puts it, on the enemy, and steps 1
        // and 2 become the two steps `rr:attack-enemy-activation` writes the
        // same way -- its step 1 word for word, and its step 3 sub-step for
        // sub-step, differing only in naming SCH where the attack names ATK.
        int round = world.Agenda.Current?.Round ?? 0;
        world.Agenda.Then(new PhaseStep(
            Steps.GiveBoostCard, round, 1, Index: seat, Subject: villain.ObjectId,
            ActivationId: world.Activation.Id));
        world.Agenda.Then(new PhaseStep(
            Steps.FlipBoostCards, round, 2, Index: seat, Subject: villain.ObjectId,
            ActivationId: world.Activation.Id));

        // **Step 3 is a step, because step 2 can stop and ask.** A `Boost`
        // ability that offers the player a choice suspends, and the threat used
        // to go onto the scheme while the question was still on the table --
        // so whatever they chose arrived after the number it was meant to
        // change. The attack activation has the same shape:
        // `FlipBoostCards` is step 3 and `CalculateAttackDamage` is step 4.
        world.Agenda.Then(new PhaseStep(
            Steps.SchemeThreat,
            round,
            3,
            Index: seat,
            Subject: villain.ObjectId,
            Seat: seat,
            ActivationId: world.Activation.Id));
    }

    /// <summary>
    /// Step 3 of a scheme activation —
    /// <c>rr:scheme-enemy-activation.step.3</c>.
    /// </summary>
    /// <remarks>
    /// "Place threat on the main scheme equal to the scheming enemy's
    /// <b>modified</b> SCH value." Modified is the word: the attack's own step
    /// reads a modified ATK, boost icons are registered as modifiers by step 2,
    /// and an attachment printing <c>SCH+</c> is one too.
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do.</param>
    /// <param name="step">The step.</param>
    /// <param name="events">Where to record what happened.</param>
    internal static void SchemeThreat(
        World world, ICardFacts facts, IThreatCardAbilities abilities, PhaseStep step,
        List<GameEvent> events)
    {
        long placed = 0;
        if (world.Agenda.Occurrence is { } occurrence)
        {
            placed = Threat.Apply(world, facts, abilities, occurrence, events);
            occurrence.Also(Steps.SchemeEnds);
        }

        // The other kind of activation ends here. A boost card's ability that
        // says "this activation" was given for *this* scheme and must not
        // survive into somebody's attack -- `rr:activation` makes a scheme an
        // activation, and `rr:activation.6` gives an activation an end.
        world.Effects.Expire(TimingPoints.EndOfActivation, events);
        if (world.Activation is { } activation)
        {
            world.FinishedActivation = activation with { ThreatPlaced = placed };
            world.Activation = null;
        }
    }

    /// <summary>Ends a scheme without placing threat when its minion left play.</summary>
    internal static void EndSchemeEarly(World world, List<GameEvent> events)
    {
        world.Effects.Expire(TimingPoints.EndOfActivation, events);
        world.FinishedActivation = world.Activation;
        world.Activation = null;
    }

}
