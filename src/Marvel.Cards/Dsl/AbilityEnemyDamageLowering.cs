using static Marvel.Cards.Dsl.AbilityBookLowering;
using static Marvel.Cards.Dsl.AbilityCostLowering;
using static Marvel.Cards.Dsl.AbilityLowering;
using static Marvel.Cards.Dsl.AbilitySelectorLowering;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Dsl;

/// <summary>Lowers the lasting heal that counts damage an identity deals to enemies.</summary>
internal static class AbilityEnemyDamageLowering
{
    // Engine spelling: {"healEachTimeYouDamageAnEnemy": {"amount": N, "until": "EndOfTurn"}}.
    internal static AbilityEnemyDamageEffect.HealOnEnemyDamage Healing(AbilityValue value, AbilityLocation location)
    {
        var fields = Fields(value, location, "amount", "until");
        string until = Text(Required(fields, "until", location), location.Child("until"));
        if (until != TimingPoints.EndOfTurn)
            throw location.Child("until").Error("the heal lasts until EndOfTurn");
        return new(PositiveInteger(Required(fields, "amount", location), location.Child("amount")), until);
    }
}
