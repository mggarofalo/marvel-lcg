from . import *

# Armored Shape

def GetAbilities() -> Sequence['Ability']:

    def armored_shape(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Hulkling"),
            attack=1,
            defense=2,
            retaliate=1
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            Event,
            armored_shape
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Hulkling", canbe_heal=True),
    ]

