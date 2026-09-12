using static Marvel.Rules.State.StateFieldCatalog;
using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>
/// The <c>fields</c> map the state digest records for one card.
/// </summary>
/// <remarks>
/// <para>
/// <b>The registered key set is part of the contract.</b> Zero-valued fields are
/// emitted, so "a port that forgets to register <c>recover</c> fails on the key
/// rather than passing by luck" (<c>docs/state-digest-v2.md</c>). An empty map
/// means the card registers nothing, never that the zone was skipped.
/// </para>
/// <para>
/// The key set is declared per kind here rather than falling out of a class
/// hierarchy. What the hierarchy bought was a <b>collision refused rather than
/// resolved</b> — two mixins claiming one key was an error, not a silent
/// last-writer-wins — and that guard is kept explicitly; see
/// <see cref="Merge"/>.
/// </para>
/// <para>
/// <b>What is measured and what is assumed.</b> The key sets below were read off
/// the fourteen face classes a real <c>rhino / spider_man / 12345</c> board
/// instantiates. On that board every card of a kind registers the same keys, with
/// one exception that is a rule rather than noise: <c>k_</c> keys are token
/// pools and appear only once a card is in play. Everything else about the
/// values is simple because setup is simple — out of play, every field is zero
/// except the traits and <c>printed_stage</c>, over all 78 out-of-play cards.
/// </para>
/// </remarks>
public static class StateFields
{
    /// <summary>Whether continuous effects may modify this field.</summary>
    public static bool IsModifiable(string field) => StateFieldCatalog.IsModifiable(field);

    /// <summary>Whether a card has a usable printed basic power.</summary>
    public static bool HasUsablePrintedPower(
        ICardFacts facts, string faceId, string attribute) =>
        StateFieldCatalog.HasUsablePrintedPower(facts, faceId, attribute);

    /// <summary>Printed attributes that supply registered state fields.</summary>
    public static IReadOnlyDictionary<string, string> FilledFrom =>
        StateFieldCatalog.FilledFrom;

    /// <summary>The default number of allies a player may control.</summary>
    public const long AllyLimit = StateFieldCatalog.AllyLimit;

    /// <summary>The default number of restricted cards a player may control.</summary>
    public const long RestrictedLimit = StateFieldCatalog.RestrictedLimit;

    // Out of play, every one of these is zero. In play, the ones this kind
    // draws from printed data are filled in by `InPlayValues`.

    /// <summary>The fields for one card, code-point ordered.</summary>
    /// <param name="card">The card.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="players">How many players are in the game.</param>
    /// <param name="inPlay">Whether the card is in play.</param>
    /// <param name="hasHeldPools">Whether it has ever registered its token pools.</param>
    /// <param name="hasFirstPlayerToken">Whether the first player token sits here.</param>
    /// <param name="world">
    /// The world, so an attached card can modify what this one prints. Null
    /// answers the printed value unmodified, which is what a caller without a
    /// board wants.
    /// </param>
    public static IReadOnlyDictionary<string, long> For(
        Card card, ICardFacts facts, int players, bool inPlay, bool hasHeldPools,
        bool hasFirstPlayerToken, World? world = null)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);

        string faceId = card.FaceId;
        var kind = FacedownDrones.Kind(card, facts);
        var fields = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            // Three namespaces merged through one guard, as the engine does:
            // a trait named `is_exhaust`, or an attribute colliding with a
            // `t_` key, must be a fault rather than a silently dropped field.
            ["is_exhaust"] = card.Ready ? 0 : 1,
        };

        // `rr:traits` — printed *and* granted. A villain wearing Super
        // Strength has the BRUTE trait, and a digest that emitted only the
        // printed list would describe a board nobody is playing.
        foreach (string trait in world is null
            ? FacedownDrones.InherentTraits(card, facts)
            : Traits.Of(world, card, facts))
        {
            Merge(fields, "t_" + trait, 1);
        }

        foreach (string key in Keys(kind, hasHeldPools))
        {
            // A registered key exists at zero until something is put on it, so
            // the card's own count is the value and the registration is what
            // decides whether the key is on the wire at all.
            Merge(fields, key, card.Tokens.TryGetValue(key, out long held) ? held : 0);
        }

        // `is_completed` is registered by a main scheme whether or not it has
        // held pools, so it is filled from the card outside the loop above.
        if (fields.ContainsKey("is_completed")
            && card.Tokens.TryGetValue("is_completed", out long completed))
        {
            fields["is_completed"] = completed;
        }

        // `printed_stage` is set when the card is built, not when it registers:
        // the milestone board records stage 2 on a villain still in the villain
        // deck, with every other printed value on it still zero.
        if (fields.ContainsKey("printed_stage"))
        {
            fields["printed_stage"] = facts.PrintedValue(faceId, "Stage", players);
        }

        if (hasHeldPools)
        {
            FillPrinted(fields, card, faceId, facts, players, world);

            // `rr:consequential-damage`. Not in `PrintedFrom` because it is not
            // an attribute of its own: the icons are stars printed inside the
            // `ATK` and `THW` values, which is why they are read rather than
            // looked up. See `CardCatalog.ConsequentialDamage`.
            foreach (var (field, power) in ConsequentialFrom)
            {
                if (fields.ContainsKey(field))
                {
                    fields[field] = facts.ConsequentialDamage(faceId, power);
                }
            }
        }

        if (inPlay)
        {
            FillInPlay(fields, card, kind, faceId, facts, players, hasFirstPlayerToken);
        }

        return fields;
    }

    /// <summary>The keys a kind registers, before any value is known.</summary>
    /// <param name="kind">The card kind.</param>
    /// <param name="hasHeldPools">Whether the card has ever registered them.</param>
    public static IEnumerable<string> Keys(CardKind kind, bool hasHeldPools)
    {
        // Leaders have their own printed kind, but `pack:mc56:leaders` says
        // they function exactly like villains. Reusing the registered villain
        // shape also holds the digest spelling stable rather than introducing
        // a parallel set of keys that could drift.
        var stateKind = CardKinds.IsVillain(kind) ? CardKind.EncounterVillain : kind;
        var registered = Registered.TryGetValue(stateKind, out var keys) ? keys : [];
        if (!hasHeldPools || !TokensOnceInPlay.TryGetValue(stateKind, out var tokens))
        {
            return registered;
        }

        return registered.Concat(tokens);
    }

    /// <summary>
    /// One variable quantity as the game currently counts it: what the card
    /// prints, plus everything modifying it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:modifiers</c>: "The game constantly checks and (if necessary)
    /// updates the count of any variable quantity that is being modified." So
    /// this is derived on every read rather than stored — an attachment that
    /// leaves play stops counting because it is no longer there to be found,
    /// and a lasting effect stops counting because it has expired.
    /// </para>
    /// <para>
    /// Two sources, and the rules do not rank them: a printed <c>ATK+</c> on an
    /// attached card (<see cref="ModifiedBy"/>) and a continuous effect naming
    /// this field (<see cref="ContinuousEffect.Kind"/>). Boost icons reach an
    /// enemy's ATK the second way — <c>rr:attack-enemy-activation.step.3.c</c>
    /// increases it for the duration of one attack, which is
    /// <c>rr:lasting-effects</c>'s own example of a duration.
    /// </para>
    /// </remarks>
    /// <param name="world">The board.</param>
    /// <param name="card">Whose quantity.</param>
    /// <param name="field">The digest's name for it, e.g. <c>attack</c>.</param>
    /// <param name="facts">The printed card data.</param>
    /// <param name="players">How many players are in the game.</param>
    public static long Modified(
        World world, Card card, string field, ICardFacts facts, int players)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(facts);

        if (Characteristics.IsLost(world, card, field))
        {
            return 0;
        }

        if (PrintedFrom.TryGetValue(field, out string? printedAttribute)
            && PowerAttributes.Contains(printedAttribute)
            && !FacedownDrones.Is(card)
            && !HasUsablePrintedPower(facts, card.FaceId, printedAttribute))
        {
            // `rr:dash-value.3`: a referenced dash is an unmodifiable zero.
            // Return before either attached or lasting modifiers are read.
            return 0;
        }

        (long value, bool hasBaseValue) = BaseValue(card, field, facts, players);

        long modified = value + Adjustments(world, card, field, facts, players);
        // `rr:modifiers.4`: clamp complete values, not adjustment-only fields.
        return hasBaseValue ? Math.Max(0, modified) : modified;
    }

    private static (long Value, bool Present) BaseValue(
        Card card, string field, ICardFacts facts, int players)
    {
        if (field == "ally_limit"
            && FacedownDrones.Kind(card, facts) is CardKind.Hero or CardKind.AlterEgo)
            return (AllyLimit, true);
        if (PrintedFrom.TryGetValue(field, out string? attribute))
            return (FacedownDrones.BaseValue(card, facts, attribute, players), true);
        // Some consumers ask for a signed adjustment and add the base themselves.
        return (0, false);
    }

    /// <summary>Printed values, filled once the card has registered.</summary>
    private static void FillPrinted(
        Dictionary<string, long> fields, Card card, string faceId,
        ICardFacts facts, int players, World? world)
    {
        foreach (var (field, attribute) in PrintedFrom)
        {
            if (!fields.ContainsKey(field) || field == "printed_stage")
            {
                continue;
            }

            if (PowerAttributes.Contains(attribute)
                && !FacedownDrones.Is(card)
                && !HasUsablePrintedPower(facts, faceId, attribute))
            {
                fields[field] = 0;
                continue;
            }

            long value = FacedownDrones.BaseValue(card, facts, attribute, players);
            if (world is not null)
            {
                value = Characteristics.IsLost(world, card, field)
                    ? 0
                    : Math.Max(
                        0,
                        value + Adjustments(world, card, field, facts, players));
            }

            fields[field] = value;
        }
    }

    /// <summary>Remaining hit points: printed, less the damage on the card.</summary>
    private static long Remaining(Card card, ICardFacts facts, int players) =>
        Math.Max(0, FacedownDrones.BaseValue(card, facts, "HP", players) - card.Damage);

    /// <summary>Everything modifying one of a card's printed values.</summary>
    private static long Adjustments(
        World world, Card card, string field, ICardFacts facts, int players)
    {
        long total = ModifiedBy.TryGetValue(field, out string? plus)
            ? Modifiers(world, card, plus, facts, players)
            : 0;

        foreach (var effect in world.Effects.Active())
        {
            if (string.Equals(effect.Kind, field, StringComparison.Ordinal)
                && effect.AppliesTo(world, card))
            {
                total += effect.Amount;
            }
        }

        return total;
    }

    /// <summary>What cards attached to this one add to a printed value.</summary>
    private static long Modifiers(
        World world, Card host, string attribute, ICardFacts facts, int players)
    {
        long total = 0;
        foreach (var area in world.Areas)
        {
            if (area.Host != host.ObjectId || !DeckTypes.IsInPlay(area.Type))
            {
                continue;
            }

            foreach (var attached in area.Cards)
            {
                total += facts.PrintedValue(attached.FaceId, attribute, players);
            }
        }

        return total;
    }

    private static void FillInPlay(
        Dictionary<string, long> fields, Card card, CardKind kind, string faceId,
        ICardFacts facts, int players, bool hasFirstPlayerToken)
    {
        switch (kind)
        {
            case CardKind.AlterEgo:
            case CardKind.Hero:
                // Both faces of one card, and `rr:form-change-form.2` says a
                // form change keeps the damage and the tokens, so the two
                // cases are one case. What differs between them is which keys
                // they registered, and `Registered` has already decided that.
                //
                // `health` is the only one still gated on being in play. The
                // recording cannot say whether it is a printed constant or a
                // pool filled on entry, because nothing in it takes damage --
                // but `01101` reaches the discard registered and at zero
                // health, so it is not filled at registration either way.
                //
                // It is remaining hit points, not printed ones: `rr:damage.1`
                // -- "when a character has damage on it equal to or in excess
                // of its hit points, it is defeated" -- so what the field means
                // is what is left. On an undamaged board the two are equal,
                // which is why the distinction is easy to lose.
                fields["health"] = Remaining(card, facts, players);
                fields["ally_limit"] = AllyLimit;
                fields["restricted_limit"] = RestrictedLimit;
                if (hasFirstPlayerToken)
                {
                    Merge(fields, "k_first_player_token", 1);
                }

                break;

            case CardKind.Minion:
                fields["health"] = Remaining(card, facts, players);
                break;

            case CardKind.MainScheme:
                // `k_threat` is *not* set from `StartingThreat` here. Starting
                // threat is placed once, when the scheme enters play, and after
                // that the tokens on the card are the truth -- a scheme that
                // re-derived its threat from print would forget every villain
                // phase.
                break;

            default:
                if (CardKinds.IsVillain(kind))
                {
                    fields["health"] = Remaining(card, facts, players);
                    break;
                }

                // Every remaining kind reaches play only by being played, which is
                // the engine's business and not setup's.
                break;
        }
    }

    // The engine's `CardFace.MergeInfo` guard (the original investigation): two sources claiming
    // one key is a fault, because the loser would vanish from the wire and a
    // missing field is invisible in a diff in a way a changed one is not.
    private static void Merge(Dictionary<string, long> fields, string key, long value)
    {
        if (!fields.TryAdd(key, value))
        {
            throw new InvalidOperationException(
                $"two sources claim the field '{key}'; the digest would silently drop one");
        }
    }
}
