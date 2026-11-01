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
}
