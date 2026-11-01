using System.Globalization;
using Marvel.Rules.Play;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The client's one wording table: every fact key's icon, short label and tooltip.</summary>
/// <remarks>
/// The engine and View send typed facts; this is the only place the client turns them into words.
/// Which facts apply is the engine's answer. The words, the icons and how short a label is are our
/// choices; a rule's tooltip words the clause its <see cref="KangNoteKind"/> cites
/// (<c>pack:mc11:game-areas</c> and the rulings named there).
/// </remarks>
internal static class TableWording
{
    private static readonly Dictionary<TableFact, Wording> Facts = new()
    {
        [TableFact.YourArea] = new("your_area", "You", "Your game area. Your cards can affect the cards here."),
        [TableFact.Acting] = new("acting", "Acting", "The villain phase is resolving in this game area now."),
        [TableFact.Departing] = new("departing", "Leaving",
            "This group leaves its game area at the end of the phase."),
        [TableFact.Destination] = new("destination", "Can join", "The leaving group can join this game area."),
        [TableFact.Retained] = new("retained", "Retained",
            "An eliminated player's Kang and stage stay here until they depart at the end of the phase."),
        [TableFact.OutOfReach] = new("out_of_reach", "Out of reach",
            "Out of reach: cards in one game area cannot affect another game area. Only stage 2B's text reaches every area."),
        [TableFact.Shared] = new("shared", "Shared", "All game areas share one encounter deck and one encounter discard pile."),
        [TableFact.StepThreat] = new("acting", "Threat", "Villain phase step 1: threat is placed on the main scheme."),
        [TableFact.StepActivation] = new("acting", "Activation",
            "Villain phase step 2: the villain and engaged minions activate."),
        [TableFact.StepEncounter] = new("acting", "Encounter", "Villain phase step 4: encounter cards are revealed."),
        [TableFact.StoredDominions] = new("dominion", "Dominions", "Kang's Dominions stored facedown under stage 4A."),
        [TableFact.AreaIsolation] = new("out_of_reach", "Separate areas",
            "Cards in one game area cannot affect another game area; only stage 2B's text reaches every area."),
        [TableFact.StepsInTurnOrder] = new("acting", "Turn order",
            "Each villain-phase step resolves for every game area in turn order before the next step begins."),
        [TableFact.EachPlayerIsArea] = new("your_area", "Each player",
            "On an encounter card, \"each player\" means each player in the same game area; a per-player value still counts {0}."),
        [TableFact.CentralTokensWaitFor4A] = new("dominion", "Tokens wait",
            "Acceleration tokens on stage 2B add no threat during the split; they carry into stage 4A."),
        [TableFact.CompletionStoresDominion] = new("dominion", "Not a loss",
            "Completing this stage does not lose the game: it stores a Kang's Dominion under stage 4A, and this group "
            + "joins another game area at the end of the phase. It is better to let Kang (II) complete his main scheme "
            + "than to let Kang (II) defeat your hero."),
        [TableFact.SeparatedRules] = new("rules", "Rules", "Show the rules that apply while the table is separated."),
    };

    /// <summary>The wording of one fact.</summary>
    internal static Wording For(TableFact fact) => Facts[fact];

    /// <summary>The fact behind a separated-table rule the engine says applies.</summary>
    internal static TableFact Fact(KangNoteKind kind) => kind switch
    {
        KangNoteKind.AreaIsolation => TableFact.AreaIsolation,
        KangNoteKind.StepsInTurnOrder => TableFact.StepsInTurnOrder,
        KangNoteKind.EachPlayerIsArea => TableFact.EachPlayerIsArea,
        KangNoteKind.SharedEncounterDeck => TableFact.Shared,
        KangNoteKind.CentralTokensWaitFor4A => TableFact.CentralTokensWaitFor4A,
        KangNoteKind.CompletionStoresDominion => TableFact.CompletionStoresDominion,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no wording for this rule"),
    };

    /// <summary>A rule's full wording; a per-player value counts the players who started the scenario.</summary>
    internal static string Rule(KangNoteKind kind, int startingPlayers) => kind == KangNoteKind.EachPlayerIsArea
        ? string.Format(CultureInfo.InvariantCulture, For(TableFact.EachPlayerIsArea).Tooltip, startingPlayers == 1
            ? "the 1 player who started the scenario"
            : $"all {Count(startingPlayers)} players who started the scenario")
        : For(Fact(kind)).Tooltip;

    /// <summary>The fact behind a reason a seat's cards cannot affect a card.</summary>
    internal static TableFact Fact(ReachReason reason) => reason switch
    {
        ReachReason.OtherGameArea => TableFact.OutOfReach,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "no wording for this reason"),
    };

    /// <summary>The fact behind a villain-phase step: 1 threat, 2 activation, 4 encounter cards.</summary>
    internal static TableFact Step(int step) => step switch
    {
        1 => TableFact.StepThreat,
        2 => TableFact.StepActivation,
        4 => TableFact.StepEncounter,
        _ => throw new ArgumentOutOfRangeException(nameof(step), step, "no villain-phase step has this number"),
    };

    /// <summary>The Kang the players face now, as a short label.</summary>
    internal static string Stage(KangTableDescriptor kang) => kang.Stage switch
    {
        1 => "Kang (I)",
        2 => "Kang (II)",
        _ => "Kang (III)",
    };

    /// <summary>Progress toward winning, in full, from the engine's stage and counts.</summary>
    internal static string Progress(KangTableDescriptor kang) => kang.Stage switch
    {
        1 when kang.PendingSplit is not null =>
            "Kang (I) is defeated. At the end of the phase each player forms their own game area against a Kang (II).",
        1 => "Defeat Kang (I) to split the table into separate game areas.",
        2 when kang.RemainingLocalKangs == 0 =>
            "No Kang (II) remains. At the end of the phase the last group leaves its game area and the table reunites "
            + "at stage 4A against Kang (III).",
        2 => $"{Count(kang.RemainingLocalKangs)} Kang (II) {(kang.RemainingLocalKangs == 1 ? "remains" : "remain")}. "
            + "A game area's group joins another when its Kang (II) is defeated or completes his stage; when no game "
            + "area remains, the table reunites at stage 4A against Kang (III).",
        _ => "Defeat Kang (III) to win.",
    } + Dominions(kang.StoredDominions);

    private static string Dominions(int stored) => stored switch
    {
        0 => "",
        1 => " 1 Kang's Dominion is stored under stage 4A.",
        _ => $" {Count(stored)} Kang's Dominions are stored under stage 4A.",
    };

    /// <summary>Why a group leaves its area: its Kang (II) was defeated, or completed his stage.</summary>
    /// <param name="kang">The departing Kang's title.</param>
    /// <param name="players">The leaving players' names; none when only retained components leave.</param>
    /// <param name="completed">Whether Kang completed his stage rather than being defeated.</param>
    /// <param name="reunion">Whether no game area remains to join, so the table reunites.</param>
    internal static string Departure(string kang, IReadOnlyList<string> players, bool completed, bool reunion) =>
        $"{kang} {(completed ? "completed his stage" : "was defeated")}: "
        + (players.Count == 0 ? "the retained components" : string.Join(" and ", players))
        + (reunion ? " leave at the end of the phase, and the table reunites at stage 4A."
            : " leave at the end of the phase and join another game area.");

    /// <summary>A game area's title, naming its players, or its former players when it is retained.</summary>
    internal static string AreaTitle(IReadOnlyList<string> players, bool retained) =>
        players.Count == 0 ? "Retained area" : string.Join(" · ", players) + (retained ? " (retained)" : "");

    /// <summary>The central column's title.</summary>
    internal const string CentralTitle = "Central";

    /// <summary>Who answers and what the decision concerns.</summary>
    internal static string DecisionContext(DecisionFrame frame) => $"Decision for {frame.SeatName}" + (frame.Subject switch
    {
        DecisionSubject.MinionActivation => " · Choosing the next minion activation",
        DecisionSubject.Inspecting => $" · Inspecting {frame.SubjectTitle}",
        DecisionSubject.Resolving => $" · Resolving {frame.SubjectTitle}",
        _ => "",
    });

    /// <summary>Whether the answering seat may decline, named by the engine's decline label.</summary>
    internal static string DecisionRequirement(DecisionFrame frame, string declineLabel) =>
        frame.Optional ? $"You may {declineLabel.ToLowerInvariant()}." : "Choose to continue.";

    /// <summary>The causes, then the engine's instruction.</summary>
    internal static string DecisionResolution(DecisionFrame frame) =>
        (frame.Causes.Count == 0 ? "" : $"Cause: {string.Join(", ", frame.Causes)}. ") + frame.Instruction;

    /// <summary>A prompt's answering seat and subject, worded.</summary>
    internal static string Context(this PromptPresentation prompt) => DecisionContext(prompt.Frame);

    /// <summary>A prompt's required-or-optional line, worded.</summary>
    internal static string Requirement(this PromptPresentation prompt) =>
        DecisionRequirement(prompt.Frame, prompt.DeclineLabel);

    /// <summary>A prompt's causes and instruction, worded.</summary>
    internal static string Resolution(this PromptPresentation prompt) => DecisionResolution(prompt.Frame);

    private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
}
