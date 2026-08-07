from . import *

# Lead from the Front

def GetAbilities() -> Sequence['Ability']:

    def lead_from_the_front(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        player = effect.targets[0].GetControlByPlayer()

        Players.EachCharacterGetsUntilPhaseEnd(
            player,
            effect,
            attack=+1,
            thwart=+1,
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            lead_from_the_front
        ).SetPlay()
        .SetTarget("Players")
    ]

