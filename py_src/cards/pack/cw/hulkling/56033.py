from . import *

# Hulk Shape

def GetAbilities() -> Sequence['Ability']:

    def hulk_shape(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hulkling"),
            attack=2,
            defense=1
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            Event,
            hulk_shape
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

