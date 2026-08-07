from . import *

# Shrink

def GetAbilities() -> Sequence['Ability']:

    def shrink(effect: 'Effect', message: 'Message.WhenPlayerPlayCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.IncreaseThreatRemove(2, effect)


    return [
        AbilityFactory.WhenPlayerPlayCard(
            AbilityType.HeroInterrupt,
            "You",
            CardFinder2("THWART", Event),
            shrink,
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

