from . import *

# Second Wind

def GetAbilities() -> Sequence['Ability']:

    def second_wind(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 4
        if effect.GetPaidResources().GetColor("B"):
            value = 5
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            second_wind
        ).SetPlay().SetLabel()
        .SetTarget(Identity, canbe_heal=True),
    ]

