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
/// They were not. The villain phase placed threat and looked; the incite
/// keyword placed threat inline and did not. A scenario one threat short of
/// its target would carry on past its own ending — every later round a round
/// that should never have been played — and thirty-three cards in the pool
/// carry incite.
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
    /// <summary>Schedule one imminent placement with its own windows.</summary>
    /// <remarks>
    /// This is a suspension point. A card interpreter may call it only after
    /// preserving any continuation that follows the placement; silently
    /// continuing the effect tree would resolve later text before interrupts to
    /// this assignment. The current card corpus places threat only at the end
    /// of its branch, so the integration can refuse every other shape loudly.
    /// </remarks>
    public static void Schedule(
        World world, Card scheme, Card? source, long amount, ThreatCause cause,
        string trigger, int player = -1, PendingAbility? resolution = null,
        Occurrence? abilityOccurrence = null) =>
        Schedule(
            world, [scheme], source, amount, cause, trigger, player,
            resolution, abilityOccurrence);

    /// <summary>Schedule the same assignment on several schemes in board order.</summary>
    public static void Schedule(
        World world, IReadOnlyList<Card> schemes, Card? source, long amount,
        ThreatCause cause, string trigger, int player = -1,
        PendingAbility? resolution = null, Occurrence? abilityOccurrence = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(schemes);
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);

        if (amount == 0)
        {
            return;
        }

        var current = world.Agenda.Current;
        int round = current?.Round ?? 0;
        int number = current?.Number ?? 0;
        int index = current?.Index ?? 0;
        PhaseStep[] placements =
        [
            .. schemes.Select((scheme, offset) =>
            {
                ArgumentNullException.ThrowIfNull(scheme);
                return new PhaseStep(
                    Steps.PlaceThreatEffect,
                    round,
                    number,
                    Index: index + offset,
                    Subject: scheme.ObjectId,
                    Seat: player,
                    Placement: new ThreatPlacement(
                        scheme.ObjectId, source?.ObjectId ?? -1, amount, cause, trigger, player),
                    Tier: resolution?.Type,
                    AbilityOrdinal: resolution?.Ordinal ?? -1,
                    AbilityOccurrence: abilityOccurrence);
            }),
        ];

        if (current is not null)
        {
            world.Agenda.Now(placements);
            return;
        }

        // A mandatory occurrence-tier ability can be called before any phase
        // is on the agenda -- scenario setup is the production case. It still
        // creates ordinary interrupt/apply/response occurrences; its caller
        // drains them through Sequence rather than applying threat inline.
        foreach (var placement in placements)
        {
            world.Agenda.Add(placement);
        }
    }

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
    /// <param name="scheme">Which scheme.</param>
    /// <param name="amount">How much to remove.</param>
    /// <param name="trigger">What caused it, for the event stream.</param>
    /// <param name="verb">What kind of change the event stream records.</param>
    /// <param name="events">Where to record what happened.</param>
    /// <param name="by">The seat whose character did it, or -1.</param>
    /// <param name="overridesCannotFrom">
    /// The source of the exact prohibition this instruction explicitly
    /// overrides, or -1. Unrelated prohibitions remain in force.
    /// </param>
    /// <returns>How much threat was removed.</returns>
    public static long Remove(
        World world, ICardFacts facts, IThreatCardAbilities abilities, Card scheme, long amount,
        string trigger, string verb, List<GameEvent> events, int by = -1,
        int overridesCannotFrom = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(events);

        long held = scheme.Tokens.GetValueOrDefault("k_threat");
        long removed = Math.Min(held, Math.Max(0, amount));
        if (removed == 0
            || !abilities.CanRemoveThreat(world, scheme, overridesCannotFrom))
        {
            return 0;
        }

        scheme.PlaceTokens("k_threat", -removed);
        events.Add(new FieldSet(scheme.ObjectId, "k_threat", held, held - removed)
        {
            Trigger = trigger, Verb = verb,
        });

        if (scheme.Area.Type == DeckType.SideSchemesArea
            && scheme.Tokens.GetValueOrDefault("k_threat") == 0)
        {
            Defeat.Scheme(world, facts, scheme, trigger, events, by);
        }

        return removed;
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
                Trigger = trigger, Verb = "Place_Threat",
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
            Trigger = "main scheme completed", Verb = "Complete",
        });

        if (world.AreaOf(DeckType.MainSchemesDeck).Cards.Count > 0)
        {
            MainScheme.Advance(world, facts, abilities, scheme, "main scheme completed", events);
            return;
        }

        world.Finish(Outcome.VillainWins);
    }
}
