namespace Marvel.Rules.State;

/// <summary>Defines registered, printed, and modifiable state fields.</summary>
public static class StateFieldCatalog
{
    internal static readonly Dictionary<CardKind, string[]> Registered = new()
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
        // **Reasoned, not measured, and the only row here that is.** No
        // recorded digest reaches hero form, so nothing can be read off a
        // board: `rr:identity.1` starts every player in alter-ego form,
        // `rhino / spider_man / 12345` is the one case carrying full
        // `step_digests` rather than hashes, and `01001a` appears in none of
        // its seven steps.
        //
        // Derived from the `AlterEgo` row, which is measured, by the two
        // differences between the faces: an alter-ego prints `REC` and a hero
        // does not, and a hero prints `ATK`, `THW` and `DEF` and an alter-ego
        // does not. `ally_limit`, `hand_size` and `restricted_limit` stay --
        // they are the player's limits and both faces carry `HS`.
        //
        // `defense` is the one key in this whole table that no recorded card
        // has. That is consistent rather than alarming: across all 58 faces and
        // 65 distinct field keys of the recording there is no `defense`,
        // because DEF is printed on hero faces alone -- allies and villains
        // have none -- and no hero face is ever faceup.
        //
        // The consequential-damage pair is deliberately absent.
        // `rr:consequential-damage` is an ally rule: "after an **ally**
        // attacks, it takes consequential damage". A hero does not.
        //
        // If a recording ever disagrees, it disagrees loudly -- an emitted key
        // set is compared whole. That is the point of writing it down instead
        // of leaving heroes registering nothing.
        [CardKind.Hero] =
        [
            "ally_limit", "attack", "defense", "hand_size", "health", "is_infinite_health",
            "restricted_limit", "retaliate", "stalwart", "steady", "surge", "thwart",
            "toughness", "vulnerable",
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
    internal static readonly Dictionary<CardKind, string[]> TokensOnceInPlay = new()
    {
        [CardKind.AlterEgo] = ["k_threat"],
        // Same pool, same reasoning as the `Hero` row above: one card, and
        // `rr:form-change-form.2` keeps its tokens across a form change, so a
        // pool that vanished when the card flipped would be the rule's
        // opposite.
        [CardKind.Hero] = ["k_threat"],
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
    internal static readonly Dictionary<string, string> PrintedFrom = new(StringComparer.Ordinal)
    {
        ["alliance"] = "Alliance",
        ["attack"] = "ATK",
        ["scheme"] = "SCH",
        ["thwart"] = "THW",
        ["guard"] = "Guard",
        ["hinder"] = "Hinder",
        ["boost_const"] = "Boost",
        ["recover"] = "REC",
        ["defense"] = "DEF",
        ["hand_size"] = "HS",
        ["escalation_threat"] = "EscalationThreat",
        ["target_threat"] = "TargetThreat",
        ["printed_stage"] = "Stage",
        ["acceleration_icon"] = "Acceleration",
        ["amplify"] = "Amplify",
        ["assault"] = "Assault",
        ["crisis"] = "Crisis",
        ["hazard"] = "Hazard",
        ["incite"] = "Incite",
        ["patrol"] = "Patrol",
        ["peril"] = "Peril",
        ["permanent"] = "Permanent",
        ["quickstrike"] = "Quickstrike",
        ["restricted"] = "Restricted",
        ["retaliate"] = "Retaliate",
        ["stalwart"] = "Stalwart",
        ["steady"] = "Steady",
        ["surge"] = "Surge",
        ["teamwork"] = "Teamwork",
        ["temporary"] = "Temporary",
        ["toughness"] = "Toughness",
        ["victory"] = "Victory",
        ["villainous"] = "Villainous",
        ["vulnerable"] = "Vulnerable",
    };

    /// <summary>Whether a name is a field the engine reads modifiers into.</summary>
    /// <remarks>
    /// <para>
    /// For the things that name a field rather than hold one — a card ability
    /// granting a keyword, which is a string in a dataset and so a typo away
    /// from granting nothing at all.
    /// </para>
    /// <para>
    /// <c>health</c> is here and is not in <see cref="PrintedFrom"/>, because
    /// remaining hit points are computed rather than printed — but
    /// <c>Damage.Health</c> sums modifiers into the printed HP, which is what
    /// "attached minion gets +2 hit points" needs. A field is modifiable if
    /// something reads modifiers for it, not if it happens to be printed.
    /// </para>
    /// </remarks>
    /// <param name="field">The field name, as this class spells it.</param>
    public static bool IsModifiable(string field) =>
        PrintedFrom.ContainsKey(field)
        || field is "health" or "ally_limit";

    // What a card attached to another adds to it. The engine's own attribute
    // names, and a closed set: 116 cards carry `ATK+`, 50 carry `SCH+`, four
    // carry `THW+`, and all but one of the 170 are attachments.
    //
    // Declarative, so no card ability is involved. Charge takes Rhino's attack
    // from 2 to 5 because it is attached to him and prints `ATK+ 3`.
    internal static readonly Dictionary<string, string> ModifiedBy = new(StringComparer.Ordinal)
    {
        ["attack"] = "ATK+",
        ["scheme"] = "SCH+",
        ["thwart"] = "THW+",
    };

    internal static readonly HashSet<string> PowerAttributes =
        new(["ATK", "THW", "DEF", "REC", "SCH"], StringComparer.Ordinal);

    /// <summary>Whether one of the five basic powers prints a usable value.</summary>
    /// <remarks>
    /// The generated card dataset omits a power whose printed value is a dash.
    /// Literal dashes remain supported for synthetic facts. This closed check
    /// applies only to powers: an omitted keyword or other quantity may still
    /// be granted by a modifier.
    /// </remarks>
    public static bool HasUsablePrintedPower(
        ICardFacts facts, string faceId, string attribute)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentException.ThrowIfNullOrEmpty(faceId);
        ArgumentException.ThrowIfNullOrEmpty(attribute);

        return PowerAttributes.Contains(attribute)
            && facts.Attributes(faceId).TryGetValue(attribute, out string? printed)
            && !string.IsNullOrWhiteSpace(printed)
            && printed is not ("-" or "–");
    }

    /// <summary>
    /// Which printed attribute fills each field, for the tests that hold this
    /// map to the card pool.
    /// </summary>
    /// <remarks>
    /// Exposed because the interesting failure is a field <b>missing</b> from
    /// it: an unmapped field reads zero forever and looks implemented. See
    /// <c>PrintedKeywordTests</c>.
    /// </remarks>
    public static IReadOnlyDictionary<string, string> FilledFrom => PrintedFrom;

    // The two fields whose value is a count of icons printed *inside* another
    // attribute rather than an attribute of its own.
    internal static readonly (string Field, string Power)[] ConsequentialFrom =
    [
        ("attack_consequential_damage", "ATK"),
        ("thwart_consequential_damage", "THW"),
    ];

    /// <summary>The default ally limit an identity registers.</summary>
    public const long AllyLimit = 3;

    /// <summary>The default restricted-upgrade limit an identity registers.</summary>
    public const long RestrictedLimit = 2;
}
