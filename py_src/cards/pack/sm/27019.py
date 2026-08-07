from . import *

# Young Love

def GetAbilities() -> Sequence['Ability']:

    def young_love(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            young_love
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]

