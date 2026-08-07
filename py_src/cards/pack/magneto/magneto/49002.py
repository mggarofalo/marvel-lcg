from . import *

# * Asteroid M

def GetAbilities() -> Sequence['Ability']:

    def asteroid_m(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)
        this.HealthUnits(effect.targets2, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            asteroid_m
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(trait="MAGNETIC", from_where=["YourDiscardPileTopmost"])
        .SetTarget2("YourIdentity", canbe_heal=True),
    ]

