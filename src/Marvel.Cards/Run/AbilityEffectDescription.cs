using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Readable effect vocabulary derived from checked ability instructions.</summary>
internal static class AbilityEffectDescription
{
    internal static string? Summary(AbilityEffect effect) => AbilitySearchDescription.Summary(effect) ?? (effect switch
    {
        AbilityEffect.ChooseCard choice => ChoiceSummary(choice),
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelWhenRevealed } =>
            "Cancel the revealed treachery's When Revealed effects",
        AbilityEffect.Draw draw => DrawSummary(draw),
        AbilityEffect.GiveStatus status => StatusAction(status.Status),
        AbilityEffect.Power power => Summary(power.Effect),
        AbilityEffect.Sequence sequence when sequence.Effects.Length == 1 =>
            Summary(sequence.Effects[0]),
        _ => null,
    });

    internal static string? Question(
        World world, string sourceFace, AbilityEffect.ChooseCard choice)
    {
        string? selection = Selection(choice);
        return selection is null
            ? null
            : $"{world.Facts.Title(sourceFace)}: {selection}";
    }

    private static string? Selection(AbilityEffect.ChooseCard choice) =>
        AbilitySearchDescription.Selection(choice) ?? AbilityPlayerChoiceDescription.Selection(choice) ?? (Action(choice.Effect) is { } action
            ? $"choose {Article(Noun(choice.From))} to {action}"
            : null);

    private static string? ChoiceSummary(AbilityEffect.ChooseCard choice)
    {
        if (Selection(choice) is not { } selection) return null;
        string summary = Imperative(selection);
        return PrintedAmount(choice.Effect) is { } amount
            ? $"{summary}. {amount}; modifiers, prevention and later effects can change the result."
            : summary;
    }

    // Printed constants describe the instruction, not its eventual result.
    // Target-specific engine previews account for the current board and modifiers.
    private static string? PrintedAmount(AbilityEffect effect) => effect switch
    {
        AbilityEffect.AttackDamage { Amount: AbilityNumber.Constant amount } =>
            $"Printed damage: {amount.Value}",
        AbilityEffect.Damage { Amount: AbilityNumber.Constant amount } =>
            $"Printed damage: {amount.Value}",
        AbilityEffect.RemoveThreat { Amount: AbilityNumber.Constant amount } =>
            $"Printed threat removal: {amount.Value}",
        AbilityEffect.Power power => PrintedAmount(power.Effect),
        AbilityEffect.Sequence { Effects.Length: 1 } sequence => PrintedAmount(sequence.Effects[0]),
        _ => null,
    };

    internal static string? Choice(AbilityEffect effect, string title) =>
        AbilitySearchDescription.Commitment(effect, title)
        ?? (Action(effect) is { } action ? $"{Imperative(action)} {title}" : null);

    private static string? Action(AbilityEffect effect) => effect switch
    {
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Discard } => "discard",
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Exhaust } => "exhaust",
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Ready } => "ready",
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelWhenRevealed } =>
            "cancel the revealed treachery's When Revealed effects",
        AbilityEffect.GiveStatus status => StatusAction(status.Status),
        AbilityEffect.AttackDamage => "attack",
        AbilityEffect.Damage => "deal damage to",
        AbilityEffect.RemoveThreat => "remove threat from",
        AbilityEffect.Power power => Action(power.Effect),
        AbilityEffect.Sequence sequence when sequence.Effects.Length == 1 =>
            Action(sequence.Effects[0]),
        _ => null,
    };

    private static string DrawSummary(AbilityEffect.Draw draw)
    {
        string count = $"{draw.Count} card{(draw.Count == 1 ? string.Empty : "s")}";
        return draw.Players switch
        {
            AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You } => $"Draw {count}",
            AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.TriggerPlayer } => $"The triggering player draws {count}",
            AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.Controller } => $"This card's controller draws {count}",
            AbilityPlayerSelection.AllPlayers => $"Each player draws {count}",
            _ => $"The designated player draws {count}",
        };
    }

    private static string StatusAction(string status) => status switch
    {
        "stunned" => "stun",
        "confused" => "confuse",
        "tough" => "give tough to",
        _ => $"give {status} to",
    };

    private static string Noun(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Discardable discardable => Noun(discardable.Cards),
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesAndSupportsYouControl } =>
            "upgrade or support",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesYouControl } => "upgrade",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.SupportsYouControl } => "support",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.Enemies
            or AbilityCardQuery.AttackableEnemies
            or AbilityCardQuery.Minions
            or AbilityCardQuery.AttackableMinions
            or AbilityCardQuery.MinionsEngagedWithYou
            or AbilityCardQuery.EnemiesEngagedWithChosenPlayer } => "enemy",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.Schemes
            or AbilityCardQuery.ThwartableSchemes
            or AbilityCardQuery.SideSchemes } => "scheme",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.Characters
            or AbilityCardQuery.CharactersYouControl
            or AbilityCardQuery.HeroesAndAllies } => "character",
        _ => "card",
    };

    private static string Article(string noun) =>
        noun.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(noun[0]))
            ? $"an {noun}"
            : $"a {noun}";

    private static string Imperative(string action) =>
        char.ToUpperInvariant(action[0]) + action[1..];
}
