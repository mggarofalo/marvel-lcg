namespace Marvel.Rules.Play;

/// <summary>A rule of the separated Kang table that a client must explain.</summary>
/// <remarks>
/// The engine names which rules apply and to which card; View supplies the wording.
/// Numeric values are wire identities: append new kinds, never renumber.
/// </remarks>
public enum KangNoteKind
{
    /// <summary>
    /// <c>pack:mc11:game-areas</c>: "Cards and components in one game area cannot affect
    /// another game area (with the exception of the text on stage 2B)."
    /// </summary>
    AreaIsolation = 1,

    /// <summary>
    /// Ruling 97d62cdb9ce436a7: each villain-phase step resolves for every game area in turn
    /// order before the next step begins.
    /// </summary>
    StepsInTurnOrder = 2,

    /// <summary>
    /// Ruling 4799b99ecc0e5dea: "'Each player' refers to each player in the same game area";
    /// ruling 0c9221b6940b4007: the per-player icon still counts the players who started.
    /// </summary>
    EachPlayerIsArea = 3,

    /// <summary>
    /// <c>pack:mc11:game-areas</c>: "they continue to use the same encounter deck and
    /// encounter discard pile."
    /// </summary>
    SharedEncounterDeck = 4,

    /// <summary>
    /// MARVEL-894 Q1: acceleration tokens on central stage 2B add no threat during the split
    /// and carry into stage 4A. Anchored to the central scheme.
    /// </summary>
    CentralTokensWaitFor4A = 5,

    /// <summary>
    /// 11009b–11012b: completing this stage stores a Kang's Dominion under stage 4A rather than
    /// losing the game; <c>pack:mc11:game-areas</c>: "It is better to let Kang (II) complete his
    /// main scheme than to let Kang (II) defeat your hero." Anchored to the local stage.
    /// </summary>
    CompletionStoresDominion = 6,
}
