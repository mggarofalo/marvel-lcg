using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Describes the attack and defense handoff from the offered engine opportunity.</summary>
internal static class DefensePromptDescription
{
    internal static string Describe(
        World world, ICardFacts facts, EnemyAttack attack, DefenseOpportunity opportunity, long attackValue)
    {
        var enemy = world.Cards[attack.Enemy];
        var choice = opportunity.Choice;
        return $"{EffectiveCards.Title(enemy, facts)} is attacking "
                + $"{EffectiveCards.Title(world.Cards[attack.Target], facts)}"
                + (opportunity.Player != attack.Player ? $" against {world.Seats[attack.Player].Name}. " : ". ")
                + $"ATK {attackValue} {AttackBoostDescription.Before(world, facts, enemy, Steps.DeclareDefender)}. "
                + (attack.IsDefended
                    ? $"{EffectiveCards.Title(world.Cards[attack.Defender], facts)} is already defending. "
                        + (choice.Required ? "Use basic defense." : "Use basic defense or keep the current defense.")
                    : choice.Required ? "Choose a ready hero or ally to defend."
                    : opportunity.Player != attack.Player
                        ? HelperDescription(opportunity)
                        : string.Empty);
    }

    private static string HelperDescription(DefenseOpportunity opportunity)
        => opportunity.HasLaterPlayer
                ? "Passing offers the next eligible player a defense opportunity."
                : "Choose a defender, or pass and leave this attack undefended.";

}
