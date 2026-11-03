namespace Marvel.Godot;

/// <summary>A fact the table states with an icon and a short label, and explains on hover.</summary>
/// <remarks><see cref="TableWording"/> holds the one wording of each fact.</remarks>
internal enum TableFact
{
    /// <summary>The inspected seat plays in this game area.</summary>
    YourArea,

    /// <summary>The engine's villain-phase focus is resolving in this game area.</summary>
    Acting,

    /// <summary>A group leaves this game area at the end of the phase.</summary>
    Departing,

    /// <summary>A leaving group can join this game area.</summary>
    Destination,

    /// <summary>The area holds an eliminated player's components until they depart.</summary>
    Retained,

    /// <summary>The inspected seat's cards cannot affect this area's cards because it is another game area.</summary>
    OutOfReach,

    /// <summary>Every game area shares this.</summary>
    Shared,

    /// <summary>Villain-phase step 1, threat.</summary>
    StepThreat,

    /// <summary>Villain-phase step 2, activation.</summary>
    StepActivation,

    /// <summary>Villain-phase step 4, encounter cards.</summary>
    StepEncounter,

    /// <summary>Kang's Dominions stored under stage 4A.</summary>
    StoredDominions,

    /// <summary>Rule: one game area cannot affect another.</summary>
    AreaIsolation,

    /// <summary>Rule: each villain-phase step resolves for every area in turn order.</summary>
    StepsInTurnOrder,

    /// <summary>Rule: "each player" on an encounter card means each player in the same game area.</summary>
    EachPlayerIsArea,

    /// <summary>Rule: acceleration tokens on stage 2B wait for stage 4A.</summary>
    CentralTokensWaitFor4A,

    /// <summary>Rule: completing a local stage stores a Dominion and moves the group.</summary>
    CompletionStoresDominion,

    /// <summary>The rules that apply while the table is separated.</summary>
    SeparatedRules,

    /// <summary>The seat holding the first-player token.</summary>
    FirstPlayer,

    /// <summary>A seat whose turn it is.</summary>
    ActiveSeat,

    /// <summary>A seat that must answer the current decision.</summary>
    AnsweringSeat,

    /// <summary>The seat this client plays.</summary>
    YourSeat,

    /// <summary>A seat the table is showing publicly.</summary>
    PublicFocus,

    /// <summary>A seat's identity is in hero form.</summary>
    HeroForm,

    /// <summary>A seat's identity is in alter-ego form.</summary>
    AlterEgoForm,

    /// <summary>Enemies engaged with a seat.</summary>
    Engaged,

    /// <summary>A decision that must be answered.</summary>
    DecisionRequired,

    /// <summary>A decision the answering seat may decline.</summary>
    DecisionOptional,

    /// <summary>A card whose effect caused a decision.</summary>
    DecisionCause,

    /// <summary>The enemy attack a decision is taken within: who attacks whom.</summary>
    Attack,

    /// <summary>The attacking enemy's current ATK.</summary>
    AttackValue,

    /// <summary>The attack's calculated damage.</summary>
    AttackDamage,

    /// <summary>Boost icons may still add to the attack.</summary>
    BoostUnresolved,

    /// <summary>All damage from the attack is prevented.</summary>
    AttackPrevented,

    /// <summary>The choice deals its printed damage.</summary>
    EffectDamage,

    /// <summary>The choice removes its printed threat.</summary>
    EffectRemoveThreat,

    /// <summary>The choice draws its printed number of cards.</summary>
    EffectDraw,

    /// <summary>The choice stuns.</summary>
    EffectStun,

    /// <summary>The choice confuses.</summary>
    EffectConfuse,

    /// <summary>The choice gives tough.</summary>
    EffectTough,

    /// <summary>The choice heals its printed damage.</summary>
    EffectHeal,

    /// <summary>The choice readies.</summary>
    EffectReady,

    /// <summary>The choice exhausts.</summary>
    EffectExhaust,

    /// <summary>The choice places its printed threat.</summary>
    EffectPlaceThreat,

    /// <summary>The choice moves its printed threat.</summary>
    EffectMoveThreat,

    /// <summary>The game ended because the final villain stage was defeated.</summary>
    OutcomeVillainDefeated,

    /// <summary>The game ended because the final main scheme was completed.</summary>
    OutcomeSchemeCompleted,

    /// <summary>The game ended because every player was eliminated.</summary>
    OutcomeEliminated,

    /// <summary>The game ended because the encounter deck and its discard pile were both empty.</summary>
    OutcomeEncounterExhausted,

    /// <summary>The game ended with no more specific cause.</summary>
    OutcomeEnded,

    /// <summary>A card the pending decision of another seat resolves.</summary>
    PendingSource,

    /// <summary>Another seat controls an offered payment source.</summary>
    SourceSeat,
}
