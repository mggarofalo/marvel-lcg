from . import *

# Under Pressure

def GetAbilities() -> Sequence['Ability']:

    def under_pressure_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        message.GiveBoostCardForThisActivation(Villain, 1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            under_pressure_boost
        ),
    ]

