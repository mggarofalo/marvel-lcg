using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Readable effect vocabulary derived from checked ability instructions.</summary>
internal static class AbilityEffectDescription
{
    internal static string? Summary(AbilityEffect effect) => AbilityBasicUseDescription.Summary(effect)
        ?? AbilityCostReductionDescription.Summary(effect)
        ?? AbilitySearchDescription.Summary(effect)
        ?? AbilityRevealDescription.Summary(effect)
        ?? AbilityReadinessDescription.Summary(effect) ?? (effect switch
    {
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.UseAttackForThwart } =>
            "Use ATK instead of THW for this thwart, including ally consequential damage",
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.ReadyUndamagedDefender } =>
            "At the end of this attack, ready your defending hero if it took no damage from that attack",
        AbilityEffect.BoostBasicUse { MatchingAlly: true } => "Add the exhausted ally's matching modified power to this basic use only; no ally consequential damage",
        AbilityEffect.ModifyPlayedEvent modifier =>
            $"Increase each {(modifier.Kind == "eventDamage" ? "damage" : "threat removal")} instance of this event by {modifier.Amount} until this play finishes",
        AbilityPricingEffect.IncreaseNextEventCost increase => $"Your next event costs {increase.Amount} additional resources; discard this obligation after that event",
        AbilityEffect.ResolveInvocation => "Resolve your captured Invocation top Special",
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.PlaySharedHeroTraitCard } =>
            "Play a card from your hand that shares a trait with your hero, reducing its cost by 1",
        AbilityEffect.Damage { Maximum: { } maximum } damage =>
            AbilityBoundedDamageDescription.Instruction(damage.Amount, maximum),
        AbilityEffect.ChooseCard choice => ChoiceSummary(choice),
        AbilityEffect.Draw draw => DrawSummary(draw),
        AbilityEffect.GiveStatus status => StatusAction(status.Status),
        AbilityEffect.ReplaceStatus status => $"Replace with {status.Status}",
        AbilityEffect.RemoveStatuses => "Remove status cards",
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
        AbilityEffect.RemoveThreat { Amount: AbilityNumber.Constant amount } threat =>
            $"Printed threat removal: {amount.Value}"
            + (threat.Maximum is { } maximum ? $" (maximum {maximum})" : ""),
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
        AbilityEffect.CardAction { Instruction: AbilityCardInstruction.MoveActiveCounter } => "move the active counter to",
        AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelWhenRevealed } =>
            "cancel the revealed treachery's When Revealed effects",
        AbilityEffect.GiveStatus status => StatusAction(status.Status),
        AbilityEffect.ReplaceStatus status => $"Replace with {status.Status}",
        AbilityEffect.RemoveStatuses => "Remove status cards",
        AbilityEffect.AttackDamage => "attack",
        AbilityEffect.Damage { Maximum: not null } => "choose damage recipients after selecting",
        AbilityEffect.Damage => "deal damage to",
        AbilityEffect.RemoveThreat => "remove threat from",
        AbilityEffect.MoveThreat { From: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            To: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
            Amount: AbilityNumber.Constant amount, All: false } =>
            $"move {amount.Value} threat onto this card from",
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
        AbilityCardSelection.Query { Kind: AbilityCardQuery.InvocationTop } => "public Invocation top",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.OtherVillainSideSchemes } => "each other villain's side scheme",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.ActiveVillainSideScheme } => "active villain's side scheme",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainOfAttachedScheme } => "villain corresponding to the attached scheme",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainsWithLeastSchemeThreat } =>
            "villain whose side scheme has the least threat",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainsWithMostSchemeThreat } =>
            "villain whose side scheme has the most threat",
        AbilityCardSelection.StatusCardsOn statuses => $"status card on {Noun(new AbilityCardSelection.Bound(statuses.Host))}",
        AbilityCardSelection.Discardable discardable => Noun(discardable.Cards),
        AbilityCardSelection.WithTrait trait => $"{(trait.Exclude ? "non-" : "")}{trait.Trait} {Noun(trait.Cards)}",
        AbilityCardSelection.WithStatus status => $"{status.Status} {Noun(status.Cards)}",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.CharactersYouControlExceptCost } =>
            "character you control other than the character exhausted for this cost",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.CardsYouControl } => "card you control",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.OtherEnvironments } => "other environment",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesAndSupportsYouControl } =>
            "upgrade or support",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesYouControl } => "upgrade",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.SupportsYouControl or AbilityCardQuery.Supports } => "support",
        AbilityCardSelection.Query { Kind: AbilityCardQuery.Enemies
            or AbilityCardQuery.AttackableEnemies
            or AbilityCardQuery.Minions
            or AbilityCardQuery.AttackableMinions
            or AbilityCardQuery.VillainAndMinionsEngagedWithYou or AbilityCardQuery.MinionsEngagedWithYou
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
