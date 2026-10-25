using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>
/// Threat placed on a scheme, and what reaching a target does.
/// </summary>
/// <remarks>
/// <para>
/// <b>Threat placed is threat placed, however it arrived.</b>
/// <c>rr:main-scheme-main-scheme-deck.2</c> completes a scheme the moment its
/// threat reaches its target threat value and says nothing at all about what
/// put the threat there — so the villain's activation, the acceleration field,
/// the incite keyword and a card's own ability are one rule and belong in one
/// place.
/// </para>
/// <para>
/// <b>Checked after each placement rather than at the end of a step.</b> A
/// scheme completed halfway through the villain's activation ends the game
/// there, and the encounter cards that would have followed are never dealt.
/// Checking at the end of the step instead would deal them first.
/// </para>
/// </remarks>
public static class Threat
{
    /// <summary>Schedule all assignments and then resume their containing ability.</summary>
    public static EffectCompletion Schedule(World world, ThreatBatch batch,
        EffectCompletion? completion = null, PhaseStep? continuation = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentOutOfRangeException.ThrowIfNegative(batch.Amount);
        var receipt = completion ?? new EffectCompletion(batch.Amount, batch.Schemes.Count);
        var current = world.Agenda.Current;
        var scheduled = batch.Schemes.Select((scheme, index) => Placement(
            batch, scheme, receipt, index, current)).ToList();
        if (scheduled.Count > 1)
        {
            // rr:triggering-condition.2: "a single interrupt window and a single
            // response window". Target assignments retain their prevention points;
            // the instruction's resulting placements share response priority.
            var responses = new Occurrence(world.Agenda.nextPlayerActionOccurrence--,
                [], Player: batch.Player);
            scheduled = scheduled.Select(step => step with
            {
                Operation = step.Require<ThreatPlacementOperation>() with { Responses = responses },
            }).ToList();
            scheduled.Add(new(new CompletedCardEffectOperation { Occurrence = responses },
                current?.Round ?? 0, current?.Number ?? 0));
        }
        if (continuation is { } resume) scheduled.Add(resume);
        if (current is not null) world.Agenda.Now(scheduled);
        else foreach (var operation in scheduled) world.Agenda.Add(operation);
        return receipt;
    }

    private static PhaseStep Placement(ThreatBatch batch, Card scheme,
        EffectCompletion completion, int part, PhaseStep? current) =>
        new(new ThreatPlacementOperation(new ThreatPlacement(
                scheme.ObjectId, batch.Source?.ObjectId ?? -1,
                batch.Amount, batch.Cause, batch.Trigger, batch.Player)
                { SchemeIncarnation = scheme.Incarnation }, completion, part)
                { Movement = batch.Movement },
            current?.Round ?? 0, current?.Number ?? 0,
            Index: (current?.Index ?? 0) + part,
            Subject: scheme.ObjectId, Seat: batch.Player);

    /// <summary>Apply the threat assignment on an agenda occurrence.</summary>
    /// <returns>The amount actually placed.</returns>
    public static long Apply(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Occurrence occurrence,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(events);

        if (occurrence.Threat is not { } placement || placement.Replaced
            || placement.Remaining <= 0)
        {
            return 0;
        }

        if (placement.Scheme < 0 || placement.Scheme >= world.Cards.Count)
        {
            throw new RulesNotImplementedException(
                "an imminent threat placement no longer names a scheme on the board");
        }

        var scheme = world.Cards[placement.Scheme];
        // rr:move.2: without a "valid source or destination ... the move cannot
        // be made". rr:leaves-play.1 calls a departed card "a new copy".
        // Interrupts can invalidate the assignment after its window opens.
        if (scheme.Area.Type is not (DeckType.MainSchemesArea or DeckType.SideSchemesArea)
            || (placement.SchemeIncarnation >= 0
                && scheme.Incarnation != placement.SchemeIncarnation)) return 0;
        long before = scheme.Tokens.GetValueOrDefault("k_threat");
        scheme.PlaceTokens("k_threat", placement.Remaining);
        events.Add(new FieldSet(
            scheme.ObjectId, "k_threat", before, before + placement.Remaining)
        {
            Trigger = placement.Trigger,
            Verb = "Place_Threat",
        });

        occurrence.Also(Steps.ThreatPlaced);
        Completed(world, facts, abilities, scheme, events);
        return placement.Remaining;
    }

    /// <summary>
    /// Removes threat from a scheme, respecting constant prohibitions and
    /// defeating a side scheme reduced to zero.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:cannot</c>: "The word 'cannot' is absolute, and cannot be
    /// countermanded by other abilities." The question is asked here so a
    /// basic thwart and a card effect cannot disagree about the same scheme.
    /// </para>
    /// <para>
    /// <c>rr:defeat</c>: "if a side scheme has no threat on it, it is
    /// defeated." Reaching zero is therefore part of removing the threat,
    /// however the removal arrived.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards continuously permit or prohibit.</param>
    /// <param name="request">The removal and its causal occurrence.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <returns>How much threat was removed.</returns>
    public static long Remove(
        World world, ICardFacts facts, IThreatCardAbilities abilities,
        ThreatRemovalRequest request, List<GameEvent> events)
        => RemoveWithOutcome(world, facts, abilities, request, events).Removed;

    /// <summary>Removes threat and reports whether a defeat procedure must finish before the containing effect resumes.</summary>
    public static ThreatRemovalResult RemoveWithOutcome(
        World world, ICardFacts facts, IThreatCardAbilities abilities,
        ThreatRemovalRequest request, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(events);

        var scheme = request.Scheme;
        long removed = RemoveTokens(world, abilities, request, events);
        if (removed == 0) return new(0, false);

        bool suspended = false;
        if (scheme.Area.Type == DeckType.SideSchemesArea
            && scheme.Tokens.GetValueOrDefault("k_threat") == 0)
        {
            suspended = Defeat.Scheme(world, facts, request, events) == Damage.Outcome.Suspended;
        }

        return new(removed, suspended);
    }

    internal static long RemoveTokens(World world, IThreatCardAbilities abilities,
        ThreatRemovalRequest request, List<GameEvent> events)
    {
        long removed = Removable(world, abilities, request);
        ApplyRemoval(request, removed, events);
        return removed;
    }

    internal static long Removable(World world, IThreatCardAbilities abilities, ThreatRemovalRequest request) =>
        (request.ByThwarting
            ? abilities.CanThwartScheme(world, request.Scheme, request.OverridesCannotFrom)
            : abilities.CanRemoveThreat(world, request.Scheme, request.OverridesCannotFrom))
            ? Math.Min(request.Scheme.Tokens.GetValueOrDefault("k_threat"), Math.Max(0, request.Amount)) : 0;

    internal static void ApplyRemoval(ThreatRemovalRequest request, long removed, List<GameEvent> events)
    {
        if (removed == 0) return;
        var scheme = request.Scheme;
        long held = scheme.Tokens.GetValueOrDefault("k_threat");
        scheme.PlaceTokens("k_threat", -removed);
        events.Add(new FieldSet(scheme.ObjectId, "k_threat", held, held - removed)
        {
            Trigger = request.Trigger,
            Verb = request.Verb,
        });

    }

    /// <summary>
    /// Places threat on a scheme, and resolves the scheme if that completed it.
    /// </summary>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do, for a stage that advances.</param>
    /// <param name="scheme">Which scheme.</param>
    /// <param name="amount">How much. Zero does nothing at all, not even an event.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Place(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Card scheme, long amount,
        string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        if (amount != 0)
        {
            long before = scheme.Tokens.GetValueOrDefault("k_threat");
            scheme.PlaceTokens("k_threat", amount);
            events.Add(new FieldSet(scheme.ObjectId, "k_threat", before, before + amount)
            {
                Trigger = trigger,
                Verb = "Place_Threat",
            });
        }

        Completed(world, facts, abilities, scheme, events);
    }

    /// <summary>
    /// Resolves a main scheme that has reached its target threat —
    /// <c>rr:main-scheme-main-scheme-deck.2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>.2.2</c> is why the completed flag is set here rather than inside
    /// <see cref="MainScheme.Advance"/>: "if the main scheme advances other
    /// than through having threat on it equal to or greater than its target
    /// threat value, that main scheme is <b>not</b> considered completed."
    /// </para>
    /// <para>
    /// <c>.2.1</c> — "if the villain completes the final stage of the main
    /// scheme deck, the villain wins the game." An empty main scheme deck is
    /// therefore an ending and not a missing card.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="abilities">What cards do, for a stage that advances.</param>
    /// <param name="scheme">Which scheme.</param>
    /// <param name="events">Where to record what happened.</param>
    public static void Completed(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Card scheme,
        List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        long held = scheme.Tokens.GetValueOrDefault("k_threat");
        long target = StateFields.Modified(
            world, scheme, "target_threat", facts, world.Players);
        if (target <= 0 || held < target || scheme.Area.Type != DeckType.MainSchemesArea)
        {
            // A side scheme has a target threat value too, and reaching it does
            // nothing: `rr:side-scheme.2` keeps a side scheme in play "until there is no
            // threat on it", which is the opposite direction.
            return;
        }

        scheme.PlaceTokens("is_completed", 1);
        events.Add(new FieldSet(scheme.ObjectId, "is_completed", 0, 1)
        {
            Trigger = "main scheme completed",
            Verb = "Complete",
        });

        if (world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0)
        {
            MainScheme.Advance(world, facts, abilities, scheme, "main scheme completed", events);
            return;
        }

        world.Finish(Outcome.VillainWins);
    }
}
