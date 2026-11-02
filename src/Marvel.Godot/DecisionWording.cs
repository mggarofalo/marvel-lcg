using System.Globalization;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>
/// The decision's words: its facts as chips, the draft's tracker labels, and the sentences that stay on hover,
/// worded from the typed facts the engine and View send.
/// </summary>
/// <remarks>The words are our choices; <see cref="TableWording"/> holds each fact's icon, label and tooltip.</remarks>
internal static class DecisionWording
{
    /// <summary>The draft tracker of opening-hand cards staged for redraw.</summary>
    internal const string DraftRedraw = "Redraw";

    /// <summary>The draft tracker of hand cards staged for end-phase discard.</summary>
    internal const string DraftDiscard = "Discard";

    /// <summary>The draft tracker of selected targets.</summary>
    internal const string DraftTargets = "Targets";

    /// <summary>The draft tracker of resources selected against the cost.</summary>
    internal const string DraftPaid = "Paid";

    /// <summary>The counted resources a payment selects beyond its cost.</summary>
    internal static readonly Wording Excess = new("", "Excess", "Resources selected beyond the cost are not applied.");

    /// <summary>Resources selected when the cost's remaining need is not known, as a short label.</summary>
    internal static string Selected(int resources) => $"{Number(resources)} selected";

    /// <summary>A paid action whose target is chosen as its effect resolves.</summary>
    internal static readonly Wording TargetLater = new("", "Target later",
        "Payment commits now. Choose a target as the effect resolves; cancelling a later draft does not refund payment.");

    /// <summary>An ordinary action being drafted: nothing is committed until its commitment control.</summary>
    internal static readonly Wording Draft = new("", "Draft", "Draft — not committed until you confirm it.");

    /// <summary>A cause chip's tooltip: what a cause is, then the card.</summary>
    internal static string DecisionCause(string cause) => $"{TableWording.For(TableFact.DecisionCause).Tooltip} {cause}.";

    /// <summary>What the decision concerns, as one short line, or empty when it names nothing.</summary>
    internal static string DecisionSubjectLine(DecisionFrame frame) => frame.Subject switch
    {
        DecisionSubject.MinionActivation => "Choosing the next minion activation",
        DecisionSubject.Inspecting => $"Inspecting {frame.SubjectTitle}",
        DecisionSubject.Resolving => $"Resolving {frame.SubjectTitle}",
        _ => "",
    };

    /// <summary>Who answers and what the decision concerns.</summary>
    internal static string DecisionContext(DecisionFrame frame) => $"Decision for {frame.SeatName}"
        + (DecisionSubjectLine(frame) is { Length: > 0 } subject ? $" · {subject}" : "");

    /// <summary>Whether the answering seat may decline, named by the engine's decline label.</summary>
    internal static string DecisionRequirement(DecisionFrame frame, string declineLabel) =>
        frame.Optional ? $"You may {declineLabel.ToLowerInvariant()}." : "Choose to continue.";

    /// <summary>The causes, then the engine's instruction.</summary>
    internal static string DecisionResolution(DecisionFrame frame) =>
        (frame.Causes.Count == 0 ? "" : $"Cause: {string.Join(", ", frame.Causes)}. ") + frame.Instruction;

    /// <summary>
    /// An attack's facts as chips: who attacks whom, its value or that its damage is prevented, and an unresolved
    /// boost.
    /// </summary>
    internal static Wording[] AttackFacts(DecisionAttack attack) =>
    [
        TableWording.For(TableFact.Attack) with
        {
            Label = $"{attack.Attacker} → {attack.Target}", Tooltip = $"{attack.Attacker} is attacking {attack.Target}.",
        },
        attack.Prevented ? TableWording.For(TableFact.AttackPrevented) : AttackValue(attack),
        .. attack.BoostUnresolved ? [TableWording.For(TableFact.BoostUnresolved)] : Array.Empty<Wording>(),
    ];

    // ATK is worded by what can still change it, as the engine's own attack sentence words it.
    private static Wording AttackValue(DecisionAttack attack)
    {
        Wording value = TableWording.For(attack.Calculated ? TableFact.AttackDamage : TableFact.AttackValue);
        if (!attack.Calculated && !attack.BoostUnresolved)
            value = value with { Tooltip = "The attacking enemy's ATK now, before defense and later effects." };
        return value with { Label = $"{value.Label} {attack.Value.ToString(CultureInfo.InvariantCulture)}" };
    }

    /// <summary>Why a finished game ended, from the engine's outcome and loss reason.</summary>
    internal static TableFact OutcomeFact(Outcome outcome, PlayerLossReason reason) => outcome switch
    {
        Outcome.PlayersWin => TableFact.OutcomeVillainDefeated,
        Outcome.VillainWins => TableFact.OutcomeSchemeCompleted,
        Outcome.PlayersLose when reason == PlayerLossReason.AllPlayersEliminated => TableFact.OutcomeEliminated,
        Outcome.PlayersLose when reason == PlayerLossReason.EncounterCardsExhausted =>
            TableFact.OutcomeEncounterExhausted,
        _ => TableFact.OutcomeEnded,
    };

    /// <summary>
    /// Another seat's pending decision as chips: each card that caused it, then each card it resolves. The waiting
    /// seat can inspect the public table and refresh; the heading's tooltip says so.
    /// </summary>
    internal static Wording[] Pending(IReadOnlyList<string> causes, IReadOnlyList<string> sources) =>
    [
        .. causes.Select(cause => TableWording.For(TableFact.DecisionCause) with { Label = cause, Tooltip = DecisionCause(cause) }),
        .. sources.Select(source => TableWording.For(TableFact.PendingSource) with
        {
            Label = source, Tooltip = $"{TableWording.For(TableFact.PendingSource).Tooltip} {source}.",
        }),
    ];

    /// <summary>The fact behind a printed effect.</summary>
    internal static TableFact EffectFact(PromptEffectKind kind) => kind switch
    {
        PromptEffectKind.Damage => TableFact.EffectDamage,
        PromptEffectKind.RemoveThreat => TableFact.EffectRemoveThreat,
        PromptEffectKind.Draw => TableFact.EffectDraw,
        PromptEffectKind.Stun => TableFact.EffectStun,
        PromptEffectKind.Confuse => TableFact.EffectConfuse,
        PromptEffectKind.Tough => TableFact.EffectTough,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "no wording for this effect"),
    };

    /// <summary>A printed effect as a chip: "Damage 8", "Draw 1", or "Stun" for a status, which has no amount.</summary>
    internal static Wording Effect(PromptEffect effect)
    {
        Wording wording = TableWording.For(EffectFact(effect.Kind));
        return effect.Kind is PromptEffectKind.Stun or PromptEffectKind.Confuse or PromptEffectKind.Tough ? wording
            : wording with { Label = $"{wording.Label} {effect.Amount.ToString(CultureInfo.InvariantCulture)}" };
    }

    /// <summary>
    /// The engine's instruction as one line when no typed fact states the decision's consequence, or empty when
    /// the attack's or the effect's chips state it, or the decision's kind and its staging tracker do.
    /// </summary>
    internal static string UnstructuredConsequence(DecisionFrame frame, PublicDecisionKind kind) =>
        frame.Attack is not null || frame.Effect is not null || StatesItsOwnConsequence(kind) ? ""
            : frame.Instruction.Replace("\n", " · ", StringComparison.Ordinal).Trim();

    // An opening hand and end-phase discards stage cards into a tracker that counts them; the rest of their
    // instruction is the rule the decision's title names, so it stays in the title's tooltip.
    private static bool StatesItsOwnConsequence(PublicDecisionKind kind) =>
        kind is PublicDecisionKind.OpeningHand or PublicDecisionKind.EndPhaseDiscards;

    /// <summary>The whole decision in words, for the question's tooltip: who answers, then the engine's instruction.</summary>
    internal static string DecisionDetail(DecisionFrame frame, string declineLabel) => string.Join("\n",
        new[] { DecisionContext(frame), DecisionRequirement(frame, declineLabel), DecisionResolution(frame) }
            .Where(line => line.Length > 0));

    /// <summary>A prompt's answering seat and subject, worded.</summary>
    internal static string Context(this PromptPresentation prompt) => DecisionContext(prompt.Frame);

    /// <summary>A prompt's required-or-optional line, worded.</summary>
    internal static string Requirement(this PromptPresentation prompt) =>
        DecisionRequirement(prompt.Frame, prompt.DeclineLabel);

    /// <summary>A prompt's causes and instruction, worded.</summary>
    internal static string Resolution(this PromptPresentation prompt) => DecisionResolution(prompt.Frame);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
