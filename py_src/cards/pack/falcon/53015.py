from . import *

# * Aero: Lei Ling

def GetAbilities() -> Sequence['Ability']:

    def aero(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        player = effect.targets[0].GetControlByPlayer()

        Players.EachCharacterGetsUntilPhaseEnd(
            player,
            effect,
            CardFinder2("AERIAL", Unit2),
            attack=+1,
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            aero
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Players"),
    ]

