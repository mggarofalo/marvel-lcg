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

    // Token pools, which a card acquires when it enters play. This is why the
    // two villain stages on the milestone board register different key sets:
    // the one in play has `k_threat` and the one in the villain deck does not.
    private static readonly Dictionary<CardKind, string[]> TokensInPlay = new()
    {
        [CardKind.AlterEgo] = ["k_threat"],
        [CardKind.MainScheme] = ["k_threat"],
        [CardKind.EncounterVillain] = ["k_threat"],
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
    /// <param name="hasFirstPlayerToken">Whether the first player token sits here.</param>
    public static IReadOnlyDictionary<string, long> For(
        Card card, ICardFacts facts, int players, bool inPlay, bool hasFirstPlayerToken)
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

        foreach (string key in Keys(kind, inPlay))
        {
            Merge(fields, key, 0);
        }

        // `printed_stage` is set when the card is built, not when it enters
        // play: the milestone board records stage 2 on a villain still in the
        // villain deck, with every other printed value on it still zero.
        if (fields.ContainsKey("printed_stage"))
        {
            fields["printed_stage"] = facts.PrintedValue(faceId, "Stage", players);
        }

        if (inPlay)
        {
            FillInPlay(fields, kind, faceId, facts, players, hasFirstPlayerToken);
        }

        return fields;
    }

    /// <summary>The keys a kind registers, before any value is known.</summary>
    /// <param name="kind">The card kind.</param>
    /// <param name="inPlay">Whether the card is in play.</param>
    public static IEnumerable<string> Keys(CardKind kind, bool inPlay)
    {
        var registered = Registered.TryGetValue(kind, out var keys) ? keys : [];
        if (!inPlay || !TokensInPlay.TryGetValue(kind, out var tokens))
        {
            return registered;
        }

        return registered.Concat(tokens);
    }

    private static void FillInPlay(
        Dictionary<string, long> fields, CardKind kind, string faceId,
        ICardFacts facts, int players, bool hasFirstPlayerToken)
    {
        switch (kind)
        {
            case CardKind.AlterEgo:
                fields["health"] = facts.PrintedValue(faceId, "HP", players);
                fields["recover"] = facts.PrintedValue(faceId, "REC", players);
                fields["hand_size"] = facts.PrintedValue(faceId, "HS", players);
                fields["ally_limit"] = AllyLimit;
                fields["restricted_limit"] = RestrictedLimit;
                if (hasFirstPlayerToken)
                {
                    Merge(fields, "k_first_player_token", 1);
                }

                break;

            case CardKind.EncounterVillain:
                fields["health"] = facts.PrintedValue(faceId, "HP", players);
                fields["attack"] = facts.PrintedValue(faceId, "ATK", players);
                fields["scheme"] = facts.PrintedValue(faceId, "SCH", players);
                break;

            case CardKind.MainScheme:
                fields["target_threat"] = facts.PrintedValue(faceId, "TargetThreat", players);
                fields["escalation_threat"] =
                    facts.PrintedValue(faceId, "EscalationThreat", players);
                fields["k_threat"] = facts.PrintedValue(faceId, "StartingThreat", players);
                break;

            default:
                // Every other kind reaches play only by being played, which is
                // the fold's business and not setup's.
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
