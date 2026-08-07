from . import *

# Vine Shield

def GetAbilities() -> Sequence['Ability']:

    def vine_shield(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainDEFForThisAttack(+3, effect)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Groot"),
            vine_shield
        ).SetCostFunc(CostFunc.Counter(Select.From("Trigger"), 1, 'growth'))
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

