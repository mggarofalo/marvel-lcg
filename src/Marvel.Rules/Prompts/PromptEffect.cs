namespace Marvel.Rules.Prompts;

/// <summary>
/// What accepting an offered choice does, as one fact a client can show without the engine's sentence: the kind of
/// effect and its printed amount.
/// </summary>
/// <param name="Kind">The effect.</param>
/// <param name="Amount">
/// The printed amount: damage dealt, threat removed, placed or moved, cards drawn or damage healed; zero for a status,
/// readying or exhausting.
/// </param>
/// <remarks>
/// The amount is the instruction's printed constant, as the description's "Printed damage" words it; modifiers,
/// prevention and later effects can change the result. Which effects carry this fact is the engine's choice: only
/// those whose amount is printed, so the fact never reads concealed information.
/// </remarks>
public sealed record PromptEffect(PromptEffectKind Kind, long Amount);
