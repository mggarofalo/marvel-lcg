from . import *

# Fruition

def GetAbilities() -> Sequence['Ability']:

    def fruition(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 2, 'growth', effect, maximum=10)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            fruition
        ).SetPlay().SetLabel()
        .SetTarget(name="Groot"),
    ]

