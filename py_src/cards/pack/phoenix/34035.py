from . import *

# Soul Sisters

def GetAbilities() -> Sequence['Ability']:

    def soul_sisters(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        this.HealthUnits(effect.targets, 2, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            soul_sisters
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]

