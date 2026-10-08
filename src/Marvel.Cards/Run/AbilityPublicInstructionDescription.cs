using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Names bounded public instructions without evaluating an unchosen branch or future selection.</summary>
internal static class AbilityPublicInstructionDescription
{
    internal static string? From(AbilityStructuralContext context, AbilityEffect effect, string? chosenTitle = null) => effect switch
    {
        AbilityEffect.Sequence sequence => Sequence(context, sequence, chosenTitle),
        AbilityEffect.ChooseCard choice => Choice(context, choice),
        AbilityEffect.CardAction action => CardAction(context, action, chosenTitle),
        AbilityEffect.ChangeForm { Player: AbilityPlayer.You, Form: Forms.AlterEgo } => "Change to alter-ego form",
        AbilityEffect.ChangeForm { Player: AbilityPlayer.You, Form: Forms.Hero } => "Change to hero form",
        _ => StateInstruction(context, effect),
    };

    private static string? Sequence(AbilityStructuralContext context, AbilityEffect.Sequence sequence, string? chosenTitle)
    {
        if (sequence.Effects.IsEmpty) return "Continue without this effect";
        string?[] instructions = [.. sequence.Effects.Select(effect => From(context, effect, chosenTitle))];
        // A partial summary would hide a later cost or consequence. Unknown
        // instructions retain the complete fallback rather than a prefix.
        return instructions.Any(string.IsNullOrWhiteSpace) ? null
            : string.Join("; then ", instructions.Select((instruction, index) => index == 0 ? instruction! : LowercaseFirst(instruction!)));
    }

    private static string? Choice(AbilityStructuralContext context, AbilityEffect.ChooseCard choice)
    {
        string? candidates = CandidateNoun(choice.From);
        string? effect = From(context, choice.Effect);
        return candidates is null || effect is null ? null
            : $"Choose {candidates}; then {LowercaseFirst(effect)}";
    }

    private static string? CandidateNoun(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesYouControl } => "an upgrade you control",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.SupportsYouControl } => "a support you control",
        AbilityCardSelection.WithTrait
        {
            Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesYouControl },
        } trait => $"a {trait.Trait.Replace('_', ' ')} upgrade you control",
        _ => null,
    };

    private static string? CardAction(AbilityStructuralContext context, AbilityEffect.CardAction action, string? chosenTitle)
    {
        string? recipient = Recipient(context, action.Selection, chosenTitle);
        if (recipient is null) return null;
        return action.Instruction switch
        {
            AbilityCardInstruction.Exhaust => $"Exhaust {recipient}",
            AbilityCardInstruction.Discard => $"Discard {recipient}",
            AbilityCardInstruction.RemoveFromGame => $"Remove {recipient} from the game",
            AbilityCardInstruction.Ready => $"Ready {recipient}",
            _ => null,
        };
    }

    private static string? Recipient(AbilityStructuralContext context, AbilityCardSelection selection, string? chosenTitle = null) => selection switch
    {
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.This } =>
            EffectiveCards.Title(context.Expressions.Source, context.Expressions.World.Facts),
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.You } =>
            EffectiveCards.Title(context.Expressions.World.Seats[AbilityCardQueries.Resolver(
                context.Expressions.Bindings)].IdentityCard, context.Expressions.World.Facts),
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen } => chosenTitle ?? "the chosen card",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesYouControl } => "all upgrades you control",
        _ => null,
    };

    private static string? StateInstruction(AbilityStructuralContext context, AbilityEffect effect) => effect switch
    {
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.PlaceAccelerationToken } =>
            "Place 1 acceleration token on the main scheme",
        AbilityEffect.GainSurge { Instances: 1 } =>
            $"Give {Source(context)} surge (deal yourself 1 facedown encounter card to reveal after this card and its responses resolve)",
        AbilityEffect.GiveStatus { Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You } } status =>
            Status(context, status),
        AbilityEffect.DiscardAtRandom
        {
            Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You },
            Count: AbilityNumber.Constant count,
        } => $"Discard {count.Value} random card{(count.Value == 1 ? "" : "s")} from your hand",
        _ => null,
    };

    private static string? Status(AbilityStructuralContext context, AbilityEffect.GiveStatus status) =>
        status.Status is "stunned" or "confused" or "tough"
            ? $"Give {Recipient(context, status.Cards)} a {status.Status} status card" : null;

    private static string Source(AbilityStructuralContext context) =>
        EffectiveCards.Title(context.Expressions.Source, context.Expressions.World.Facts);

    private static string LowercaseFirst(string text) => char.ToLowerInvariant(text[0]) + text[1..];
}
