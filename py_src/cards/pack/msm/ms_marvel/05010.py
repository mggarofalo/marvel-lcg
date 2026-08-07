from . import *

# Embiggen!

def GetAbilities() -> Sequence['Ability']:

    def embiggen(effect: 'Effect', message: 'Message.WhenPlayerPlayCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.IncreaseDamage(2, effect)


    return [
        AbilityFactory.WhenPlayerPlayCard(
            AbilityType.HeroInterrupt,
            "You",
            CardFinder2("ATTACK", Event),
            embiggen,
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

