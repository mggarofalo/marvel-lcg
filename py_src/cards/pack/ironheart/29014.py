from . import *

# * Cloud 9: Abby Boylen

def GetAbilities() -> Sequence['Ability']:

    def cloud_9(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = effect.targets[0].GetControlByPlayer()

        Players.EachCharacterGetsUntilPhaseEnd(
            player,
            effect,
            CardFinder2("AERIAL", Unit2),
            thwart=+1
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            cloud_9
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Players"),
    ]

