using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Readable effect vocabulary derived from checked ability instructions.</summary>
internal static class AbilityEffectDescription
{
    internal static string? Summary(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChooseCard choice when Action(choice.Effect) is { } action =>
            $"Choose {Article(Noun(choice.From))} to {action}",
        AbilityEffect.GiveStatus status => StatusAction(status.Status),
        AbilityEffect.Power power => Summary(power.Effect),
        AbilityEffect.Sequence sequence when sequence.Effects.Length == 1 =>
            Summary(sequence.Effects[0]),
        _ => null,
    };

    internal static string? Question(
        World world, string sourceFace, AbilityEffect.ChooseCard choice)
    {
        string? action = Action(choice.Effect);
        return action is null
            ? null
            : $"{world.Facts.Title(sourceFace)}: choose {Article(Noun(choice.From))} to {action}";
    }

    internal static string? Choice(AbilityEffect effect, string title) =>
        Action(effect) is { } action ? $"{Imperative(action)} {title}" : null;

    private static string? Action(AbilityEffect effect) => effect switch
    {
        AbilityEffect.GiveStatus status => StatusAction(status.Status),
        AbilityEffect.Power power => Action(power.Effect),
        AbilityEffect.Sequence sequence when sequence.Effects.Length == 1 =>
            Action(sequence.Effects[0]),
        _ => null,
    };

    private static string StatusAction(string status) => status switch
    {
        "stunned" => "stun",
        "confused" => "confuse",
        "tough" => "give tough to",
        _ => $"give {status} to",
    };

    private static string Noun(AbilityCardSelection selection) => selection switch
    {
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
