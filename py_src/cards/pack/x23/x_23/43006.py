from . import *

# Regenerative Longevity

def GetAbilities() -> Sequence['Ability']:

    def regenerative_longevity(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)

    def find(effect: 'Effect', face: 'CardFace') -> bool:
        if Identity.IsType(face) and face.GetControlByPlayer() == effect.GetInitiator():
            return True
        if face.IsName("Honey Badger"):
            return True
        return False

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            regenerative_longevity
        ).SetPlay().SetLabel()
        .SetTarget("YouControlUnit", check_fn=find, repeat_rules="Sustained", range=(1, 4)),
    ]

