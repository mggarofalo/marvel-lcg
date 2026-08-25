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
/// In the Python engine the key set falls out of a class hierarchy: nine
/// <c>GetInfoDict</c> overrides and about forty attribute mixins, merged with
/// the more derived class winning and a <b>collision refused rather than
/// resolved</b>. C# has no multiple inheritance, so the sets are declared per
/// kind here and the collision guard is kept — see <see cref="Merge"/>.
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
    // Out of play, every one of these is zero. In play, the ones this kind
    // draws from printed data are filled in by `InPlayValues`.
    private static readonly Dictionary<CardKind, string[]> Registered = new()
    {
        [CardKind.Insert] = [],
        // A status card registers nothing of its own: the recorded Tough on
        // the milestone board carries `is_exhaust` and not one field more,
        // and the villain it is attached to keeps `toughness` at zero. The
        // status *is* the card.
        [CardKind.Status] = [],
        [CardKind.Resource] = ["surge"],
        [CardKind.Event] = ["surge"],
        [CardKind.Support] = ["permanent", "surge"],
        [CardKind.Treachery] = ["boost_const", "incite", "peril", "surge", "victory"],
        [CardKind.Upgrade] =
            ["crisis", "hazard", "permanent", "restricted", "surge", "temporary", "victory"],
        [CardKind.Obligation] =
            ["acceleration_icon", "boost_const", "hazard", "incite", "peril", "surge", "victory"],
        [CardKind.EncounterSideScheme] =
        [
            "acceleration_icon", "amplify", "assault", "boost_const", "crisis", "hazard",
            "incite", "peril", "permanent", "surge", "victory",
        ],
        [CardKind.Attachment] =
        [
            "acceleration_icon", "amplify", "boost_const", "crisis", "hazard", "incite",
            "peril", "permanent", "stalwart", "steady", "surge", "toughness", "victory",
            "vulnerable",
        ],
        [CardKind.Ally] =
        [
            "acceleration_icon", "amplify", "attack", "attack_consequential_damage", "hazard",
            "health", "is_infinite_health", "retaliate", "stalwart", "steady", "surge", "thwart",
            "thwart_consequential_damage", "toughness", "victory", "vulnerable",
        ],
        [CardKind.AlterEgo] =
        [
            "ally_limit", "hand_size", "health", "is_infinite_health", "recover",
            "restricted_limit", "retaliate", "stalwart", "steady", "surge", "toughness",
            "vulnerable",
        ],
        [CardKind.Minion] =
        [
            "acceleration_icon", "amplify", "attack", "boost_const", "engaged_with", "guard",
            "hazard", "health", "incite", "is_infinite_health", "patrol", "peril", "quickstrike",
            "retaliate", "scheme", "stalwart", "steady", "surge", "teamwork", "toughness",
            "victory", "villainous", "vulnerable",
        ],
        [CardKind.MainScheme] =
        [
            "amplify", "assault", "escalation_threat", "hazard", "is_completed", "printed_stage",
            "surge", "target_threat",
        ],
        [CardKind.EncounterVillain] =
        [
            "acceleration_icon", "amplify", "attack", "hazard", "health", "is_infinite_health",
            "printed_stage", "retaliate", "scheme", "stalwart", "steady", "surge", "toughness",
            "victory", "vulnerable",
        ],
    };

    // Token pools, acquired when a card enters play and never given back. This
    // is why the two villain stages on the milestone board register different
    // key sets -- the one in play has `k_threat` and the one still in the
    // villain deck does not -- and why a revealed treachery keeps its
    // `k_threat` from the discard pile two steps later.
    //
    // Measured, kind by kind, on the recorded board. A status card enters play
    // and registers nothing, so this is not "everything in play".
    private static readonly Dictionary<CardKind, string[]> TokensOnceInPlay = new()
    {
        [CardKind.AlterEgo] = ["k_threat"],
        [CardKind.MainScheme] = ["k_threat"],
        [CardKind.EncounterVillain] = ["k_threat"],
        [CardKind.Treachery] = ["k_threat"],
        [CardKind.Minion] = ["k_threat"],
        [CardKind.Attachment] = ["k_threat"],
    };

    // A registered field whose value is printed on the card. Filled when the
    // card registers -- which is not the same as being in play, and the
    // recording forces them apart: `01101` Hydra Mercenary reaches the discard
    // pile with `attack: 1` and `guard: 1` filled and `health: 0`, having only
    // ever passed through the boosting area.
    private static readonly Dictionary<string, string> PrintedFrom = new(StringComparer.Ordinal)
    {
        ["attack"] = "ATK",
        ["scheme"] = "SCH",
        ["thwart"] = "THW",
        ["guard"] = "Guard",
        ["boost_const"] = "Boost",
        ["recover"] = "REC",
        ["hand_size"] = "HS",
        ["escalation_threat"] = "EscalationThreat",
        ["target_threat"] = "TargetThreat",
        ["printed_stage"] = "Stage",
    };

    // What a card attached to another adds to it. The engine's own attribute
    // names, and a closed set: 116 cards carry `ATK+`, 50 carry `SCH+`, four
    // carry `THW+`, and all but one of the 170 are attachments.
    //
    // Declarative, so no card ability is involved. Charge takes Rhino's attack
    // from 2 to 5 because it is attached to him and prints `ATK+ 3`.
    private static readonly Dictionary<string, string> ModifiedBy = new(StringComparer.Ordinal)
    {
        ["attack"] = "ATK+",
        ["scheme"] = "SCH+",
        ["thwart"] = "THW+",
    };

    /// <summary>The default ally limit an identity registers.</summary>
    public const long AllyLimit = 3;

    /// <summary>The default restricted-upgrade limit an identity registers.</summary>
    public const long RestrictedLimit = 2;

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
        var kind = facts.Kind(faceId);
        var fields = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            // Three namespaces merged through one guard, as the engine does:
            // a trait named `is_exhaust`, or an attribute colliding with a
            // `t_` key, must be a fault rather than a silently dropped field.
            ["is_exhaust"] = card.Ready ? 0 : 1,
        };

        foreach (string trait in facts.Traits(faceId))
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
        var registered = Registered.TryGetValue(kind, out var keys) ? keys : [];
        if (!hasHeldPools || !TokensOnceInPlay.TryGetValue(kind, out var tokens))
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

        long value = PrintedFrom.TryGetValue(field, out string? attribute)
            ? facts.PrintedValue(card.FaceId, attribute, players)
            : 0;
        return value + Adjustments(world, card, field, facts, players);
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

            long value = facts.PrintedValue(faceId, attribute, players);
            if (world is not null)
            {
                value += Adjustments(world, card, field, facts, players);
            }

            fields[field] = value;
        }
    }

    /// <summary>Remaining hit points: printed, less the damage on the card.</summary>
    private static long Remaining(Card card, string faceId, ICardFacts facts, int players) =>
        Math.Max(0, facts.PrintedValue(faceId, "HP", players) - card.Damage);

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
                && effect.Affects == card.ObjectId)
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
                // `health` is the only one still gated on being in play. The
                // recording cannot say whether it is a printed constant or a
                // pool filled on entry, because nothing in it takes damage --
                // but `01101` reaches the discard registered and at zero
                // health, so it is not filled at registration either way.
                //
                // It is remaining hit points, not printed ones: `rr:damage.1`
                // -- "when a character has damage on it equal to or in excess
                // of its hit points, it is defeated" -- and the Python engine's
                // `health` property is the same subtraction. On every recorded
                // board the subtrahend is zero, so this is the printed value
                // there and the recording cannot tell the two apart.
                fields["health"] = Remaining(card, faceId, facts, players);
                fields["ally_limit"] = AllyLimit;
                fields["restricted_limit"] = RestrictedLimit;
                if (hasFirstPlayerToken)
                {
                    Merge(fields, "k_first_player_token", 1);
                }

                break;

            case CardKind.EncounterVillain:
                fields["health"] = Remaining(card, faceId, facts, players);
                break;

            case CardKind.MainScheme:
                // `k_threat` is *not* set from `StartingThreat` here. Starting
                // threat is placed once, when the scheme enters play, and after
                // that the tokens on the card are the truth -- a scheme that
                // re-derived its threat from print would forget every villain
                // phase.
                break;

            default:
                // Every other kind reaches play only by being played, which is
                // the engine's business and not setup's.
                break;
        }
    }

    // The engine's `CardFace.MergeInfo` guard (MARVEL-49): two sources claiming
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
