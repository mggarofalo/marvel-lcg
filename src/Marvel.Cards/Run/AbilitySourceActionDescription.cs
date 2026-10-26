using Marvel.Cards.Dsl;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Compact action captions from public printed instruction structure, without forecasting resolution.</summary>
internal static class AbilitySourceActionDescription
{
    internal static string? Caption(AbilityEffect effect) => AbilityPrintedTextDescription.Caption(effect) ?? (effect switch
    {
        AbilityEffect.ChooseCard choice => Caption(choice.Effect),
        AbilityEffect.Power { Effect: AbilityEffect.AttackDamage damage } => Attack(damage.Amount),
        AbilityEffect.GrantTrait { Cards: AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.You }, Until: { } until } grant =>
            Duration(until) is { } duration ? $"Gain {grant.Trait} until {duration}" : null,
        _ => null,
    });

    private static string? Attack(AbilityNumber amount) => amount switch
    {
        AbilityNumber.Constant number => $"Attack: {number.Value} damage",
        AbilityNumber.Conditional
        {
            Test: AbilityCondition.CardText { Property: AbilityCardTextProperty.Trait,
                Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You } } trait,
            Then: AbilityNumber.Constant increased,
            Else: AbilityNumber.Constant baseline,
        } => $"Attack: {baseline.Value} damage ({increased.Value} with {trait.Text})",
        _ => null,
    };

    private static string? Duration(string until) => until switch
    {
        TimingPoints.EndOfTurn => "end of turn",
        TimingPoints.EndOfPlayerPhase => "end of player phase",
        TimingPoints.EndOfVillainPhase => "end of villain phase",
        TimingPoints.EndOfPhase => "end of phase",
        TimingPoints.EndOfRound => "end of round",
        TimingPoints.EndOfAttack => "end of attack",
        TimingPoints.EndOfActivation => "end of activation",
        _ => null,
    };
}
