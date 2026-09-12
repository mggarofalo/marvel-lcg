using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Maintains registered effects and settles health changes when they end.</summary>
internal static class ContinuousEffectRegistry
{
    /// <summary>Put an effect into force.</summary>
    /// <remarks>
    /// Registering the same entry twice registers it twice, and that is correct:
    /// <c>rr:ability.10</c> — "If multiple instances of the same constant
    /// ability are in play, each instance affects the game independently."
    /// </remarks>
    /// <param name="effects">The effect collection being updated.</param>
    /// <param name="effect">What is now in force.</param>
    /// <returns>A handle that removes it again.</returns>
    internal static ContinuousEffects.Registration RegisterCore(this ContinuousEffects effects, ContinuousEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        var entry = new Entry(effect);
        effects.entries.Add(entry);
        return new ContinuousEffects.Registration(effects, entry);
    }

    /// <summary>Ends direct lasting effects on a card that leaves play.</summary>
    /// <remarks>
    /// A card that leaves play and later returns is a new instance of that
    /// card. Direct lasting effects therefore do not follow its object id
    /// across the zone boundary. Live-set effects use <see cref="ContinuousEffect.Scope"/>
    /// and remain in force so newly entering cards can still join that set.
    /// </remarks>
    internal static void CardLeftPlayCore(this ContinuousEffects effects, Card card)
    {
        ArgumentNullException.ThrowIfNull(card);

        effects.entries.RemoveAll(entry =>
            entry.Effect.Source == EffectSource.LastingEffect
            && entry.Effect.Card is not null
            && entry.Effect.Affects == card.ObjectId
            && entry.Effect.Scope.Length == 0);
    }

    /// <summary>
    /// Grants a modified field to every character one player controls for a
    /// stated duration.
    /// </summary>
    /// <remarks>
    /// The player is anchored by their identity and the affected set is read
    /// live. <c>rr:lasting-effects.4</c> therefore includes an ally that enters
    /// play after this effect was registered instead of freezing the set at
    /// resolution time.
    /// </remarks>
    internal static ContinuousEffects.Registration GrantToCharactersControlledByCore(
        this ContinuousEffects effects, Card source, int player, string field, long amount, string until)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(until);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, effects.world.Players);

        if (!StateFieldCatalog.IsModifiable(field))
        {
            throw new RulesNotImplementedException(
                $"'{field}' is not a field the engine can modify");
        }

        return effects.RegisterCore(new ContinuousEffect(
            EffectSource.LastingEffect,
            field,
            Amount: amount,
            Card: source.ObjectId,
            Affects: effects.world.Seats[player].IdentityCard.ObjectId,
            Lasts: Duration.UntilEndOf(until),
            Scope: ContinuousEffect.CharactersControlledBy));
    }

    /// <summary>Everything actually in force on this board, right now.</summary>
    /// <remarks>
    /// <para>
    /// Read afresh every time rather than cached onto the board, which is what
    /// <c>rr:modifiers</c> and <c>rr:lasting-effects.3</c> both describe. Cheap
    /// and called often is the intended shape.
    /// </para>
    /// <para>
    /// <b>Two sources, and only one of them is a list.</b> Lasting and delayed
    /// effects were registered by whatever created them. Constant abilities
    /// never were: <c>rr:ability</c> makes one active "as soon as its card
    /// enters play" and <c>rr:ability.9</c> makes a conditional one active
    /// "anytime the specific condition is met", so both are read off the board
    /// here, card by card, through
    /// <c>ICardConstantAbilities.Constant</c>. Nothing has to remember to register
    /// one when a card arrives or to dispose it when the card goes, and there
    /// is therefore no path into play on which a constant ability is quietly
    /// missing.
    /// </para>
    /// </remarks>
    internal static IReadOnlyList<ContinuousEffect> ActiveCore(this ContinuousEffects effects)
    {
        var registered = effects.entries
            .Where(entry => !effects.suppressed.Contains(entry))
            .Select(entry => entry.Effect)
            .Where(effect => effects.InForce(effect))
            .ToList();
        var constants = effects.ExcludingSuppressedConstants(
            effects.deriving ? effects.assumedConstants : effects.Constant());
        return [.. registered, .. constants];
    }

    private static IReadOnlyList<ContinuousEffect> ExcludingSuppressedConstants(
        this ContinuousEffects effects,
        IReadOnlyList<ContinuousEffect> constants)
    {
        if (effects.suppressedConstants.Count == 0)
        {
            return constants;
        }

        var visible = constants.ToList();
        foreach (var suppressedEffect in effects.suppressedConstants)
        {
            int index = visible.FindIndex(effect => effect == suppressedEffect);
            if (index >= 0)
            {
                visible.RemoveAt(index);
            }
        }
        return visible;
    }

    /// <summary>What every constant ability in play is doing right now.</summary>
    /// <remarks>
    /// <para>
    /// <b>Constants settle together.</b> <c>rr:modifiers.2</c> treats all
    /// modifiers as simultaneous, and one constant may depend on an attribute
    /// another constant grants. Each pass therefore reads the previous complete
    /// pass until two answers agree. An answer that cycles or keeps changing is
    /// refused rather than taken from an arbitrary intermediate pass.
    /// </para>
    /// </remarks>
    private static List<ContinuousEffect> Constant(this ContinuousEffects effects)
    {
        var seen = new List<IReadOnlyList<ContinuousEffect>>();
        effects.assumedConstants = [];

        try
        {
            // The card vocabulary is finite, but a malformed dependency can change
            // a numeric modifier forever without repeating a prior list. Sixty-four
            // full passes is the engine's chosen guard against that non-game state;
            // ordinary dependency chains settle in one pass per link.
            for (int pass = 0; pass < 64; pass++)
            {
                var found = new List<ContinuousEffect>();
                effects.deriving = true;
                try
                {
                    foreach (var card in effects.world.Cards)
                    {
                        if (DeckTypes.IsInPlay(card.Area.Type)
                            && !effects.departing.Contains(card.ObjectId))
                        {
                            found.AddRange(effects.world.ConstantAbilities.Constant(effects.world, card));
                        }
                    }
                }
                finally
                {
                    effects.deriving = false;
                }

                if (found.SequenceEqual(effects.assumedConstants))
                {
                    return found;
                }

                if (seen.Any(previous => previous.SequenceEqual(found)))
                {
                    throw new RulesNotImplementedException(
                        "the constant abilities do not settle on one simultaneous effect list");
                }

                seen.Add([.. effects.assumedConstants]);
                effects.assumedConstants = found;
            }

            throw new RulesNotImplementedException(
                "the constant abilities did not settle after 64 simultaneous passes");
        }
        finally
        {
            effects.deriving = false;
            effects.assumedConstants = [];
        }
    }

    /// <summary>
    /// End every lasting effect whose duration names this timing point.
    /// </summary>
    /// <remarks>
    /// <c>rr:lasting-effects.5</c>: "A lasting effect expires as soon as the
    /// timing point specified by its duration is reached." The villain phase's
    /// own step 6 is one of these — <c>rr:villain-phase.step.6.a</c>, where
    /// everything lasting "until the end of the round" ends.
    /// </remarks>
    /// <param name="effects">The effect collection being updated.</param>
    /// <param name="timingPoint">The point that has been reached.</param>
    /// <param name="events">Where to record state changes caused by restored constants.</param>
    /// <returns>How many effects ended.</returns>
    internal static int ExpireCore(this ContinuousEffects effects, string timingPoint, List<GameEvent>? events = null)
    {
        ArgumentNullException.ThrowIfNull(timingPoint);
        var expired = effects.entries.Where(entry => string.Equals(
            entry.Effect.Lasts?.Until, timingPoint, StringComparison.Ordinal)).ToList();
        effects.End(expired, timingPoint, events);
        return expired.Count;
    }

    /// <summary>
    /// Apply one use of an effect, and end it if that was its last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bound that is not a timing point: "reduce the cost of the next card
    /// you play by 1" is spent by a card being played, whenever that happens.
    /// An effect with no <see cref="Duration.Uses"/> is unlimited and this only
    /// reports that it applied.
    /// </para>
    /// <para>
    /// When the same effect is registered twice, one of the two is spent and
    /// the other is not, which is what <c>rr:ability.10</c> asks for: each
    /// instance affects the game independently.
    /// </para>
    /// </remarks>
    /// <param name="effects">The effect collection being updated.</param>
    /// <param name="effect">The effect being applied.</param>
    /// <returns>False when no registered copy of it had a use left.</returns>
    internal static bool UseCore(this ContinuousEffects effects, ContinuousEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);

        var entry = effects.entries.FirstOrDefault(
            candidate => candidate.Effect == effect && candidate.Remaining != 0);
        if (entry is null)
        {
            return false;
        }

        if (entry.Remaining is int remaining)
        {
            if (remaining <= 1)
            {
                effects.End([entry], "continuous effect used", events: null);
            }
            else
            {
                entry.Remaining = remaining - 1;
            }
        }

        return true;
    }

    /// <summary>
    /// Resolve every delayed effect waiting on a condition that has just
    /// occurred, and end those that were waiting for the last time.
    /// </summary>
    /// <remarks>
    /// <c>rr:delayed-effect.1</c> — they resolve "automatically and immediately
    /// after their specified timing point or future condition occurs or becomes
    /// true, and before responses to that point or condition may be used". So
    /// this is called at the occurrence, not from the response window.
    /// <c>rr:delayed-effect.2</c> is why the result is a plain list rather than
    /// anything that goes into a window: "it is not treated as a new triggered
    /// ability, even if the delayed effect was originally created by a triggered
    /// ability".
    /// </remarks>
    /// <param name="effects">The effect collection being updated.</param>
    /// <param name="condition">The condition that has occurred.</param>
    internal static IReadOnlyList<ContinuousEffect> OccurCore(this ContinuousEffects effects, string condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        var due = effects.entries
            .Where(entry => string.Equals(
                entry.Effect.Lasts?.OnCondition, condition, StringComparison.Ordinal))
            .ToList();

        var ending = due.Where(entry => entry.Remaining is null or <= 1).ToList();
        effects.End(ending, condition, events: null);
        foreach (var entry in due.Except(ending))
        {
            entry.Remaining -= 1;
        }

        return [.. due.Select(entry => entry.Effect)];
    }

    private static bool InForce(this ContinuousEffects effects, ContinuousEffect effect)
    {
        if (effect.Source != EffectSource.ConstantAbility)
        {
            return true;
        }

        // Derived rather than deregistered. See the class remarks.
        return effect.Card is int card
            && card >= 0
            && card < effects.world.Cards.Count
            && DeckTypes.IsInPlay(effects.world.Cards[card].Area.Type);
    }

    internal static void Remove(this ContinuousEffects effects, Entry entry)
    {
        if (effects.entries.Contains(entry))
        {
            effects.End([entry], "continuous effect ended", events: null);
        }
    }

    private static void End(
        this ContinuousEffects effects,
        List<Entry> ending,
        string trigger,
        List<GameEvent>? events)
    {
        if (ending.Count == 0)
        {
            return;
        }

        Card[] candidates = effects.LostUsesCandidates();
        ContinuousEffectDeparture constantsEnding;
        effects.suppressed.UnionWith(ending);
        try
        {
            var restoredUses = effects.RestoredUsesAfter(candidates);
            constantsEnding = effects.PreflightDepartures(
                [.. restoredUses, .. effects.LethalAfterHealthEnds(
                    ending.Select(entry => entry.Effect))],
                includeHostedCards: true, moveRoots: true);
        }
        finally
        {
            effects.suppressed.ExceptWith(ending);
        }

        foreach (var entry in ending)
        {
            effects.entries.Remove(entry);
        }

        var sink = events ?? [];
        using var departure = constantsEnding.Begin();
        constantsEnding.Complete(trigger, sink);
    }

    internal static Card[] LostUsesCandidates(this ContinuousEffects effects) => effects.world.Cards.Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)
            && !FacedownDrones.Is(card)
            && Characteristics.IsLost(effects.world, card, "uses")
            && effects.world.CounterPools.CounterPool(effects.world, card)?.Uses == true
            && card.Tokens
                .Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value) == 0).ToArray();

    internal static Card[] RestoredUsesAfter(this ContinuousEffects effects, Card[] candidates)
    {
        var restored = candidates
            .Where(card => !Characteristics.IsLost(effects.world, card, "uses"))
            .ToArray();
        var restoredIds = restored.Select(card => card.ObjectId).ToHashSet();
        return restored.Where(card => !effects.HasHostedAncestor(card, restoredIds)).ToArray();
    }

    internal static Card[] LethalAfterHealthEnds(this ContinuousEffects effects, IEnumerable<ContinuousEffect> ending)
    {
        var health = ending.Where(effect =>
                string.Equals(effect.Kind, "health", StringComparison.Ordinal)
                && effect.Amount > 0)
            .ToArray();
        if (health.Length == 0)
        {
            return [];
        }

        return effects.world.Cards.Where(card =>
                DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(FacedownDrones.Kind(card, effects.world.Facts))
                && StateFields.Modified(
                    effects.world, card, "is_infinite_health", effects.world.Facts, effects.world.Players) <= 0
                && health.Where(effect => effect.AppliesTo(effects.world, card))
                    .Sum(effect => effect.Amount) is long lost and > 0
                && card.Damage < Play.DamagePlacement.Health(effects.world, effects.world.Facts, card) + lost
                && card.Damage >= Play.DamagePlacement.Health(effects.world, effects.world.Facts, card))
            .ToArray();
    }


    internal static bool HasHostedAncestor(this ContinuousEffects effects, Card card, HashSet<int> candidates)
    {
        int host = card.Area.Host;
        var seen = new HashSet<int> { card.ObjectId };
        bool candidateAncestor = false;
        while (host >= 0)
        {
            if (!seen.Add(host))
            {
                throw new RulesNotImplementedException(
                    $"attachment {host} forms a hosting cycle");
            }
            if (candidates.Contains(host))
            {
                candidateAncestor = true;
            }
            host = host < effects.world.Cards.Count ? effects.world.Cards[host].Area.Host : -1;
        }
        return candidateAncestor;
    }
}
