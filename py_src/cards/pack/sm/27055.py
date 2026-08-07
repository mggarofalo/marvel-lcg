from . import *

# * Sky-Destroyer

def GetAbilities() -> Sequence['Ability']:

    def sky_destroyer(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        # FAQ: Sky-Destroyer can be exhausted and let you deal 2 damage after playing it.
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("S.H.I.E.L.D"),
            sky_destroyer,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

