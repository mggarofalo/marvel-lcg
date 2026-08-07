from . import *

# Outwit

def GetAbilities() -> Sequence['Ability']:

    def outwit(effect: 'Effect', message: 'Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        value = GetTuckCardSameSetSize(initiator, message.schemes, effect)
        message.GainTHWForThisThwart(value, effect)


    return [
        AbilityFactory.WhenUnitMakeThwart(
            AbilityType.HeroInterrupt,
            CardFinder(name="Silk"),
            None,
            outwit,
            is_basic_thwart=True
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
    ]

