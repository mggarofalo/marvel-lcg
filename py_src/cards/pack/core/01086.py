from . import *

# First Aid

def GetAbilities() -> Sequence['Ability']:

    def first_aid(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            first_aid
        ).SetPlay()
        .SetTarget(Unit2, canbe_heal=True)
    ]
