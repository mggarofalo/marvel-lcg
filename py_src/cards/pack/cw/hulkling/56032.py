from . import *

# Winged Shape

def GetAbilities() -> Sequence['Ability']:

    def winged_shape(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hulkling"),
            thwart=1,
            attack=1,
            defense=1,
            trait="AERIAL"
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            Event,
            winged_shape
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2),
    ]

