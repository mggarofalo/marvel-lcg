from . import *

# Pincer Maneuver

def GetAbilities() -> Sequence['Ability']:

    def pincer_maneuver(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.RemoveThreatFromSchemes([this], 3, effect)


    return [
        ExhaustTheMilano(
            None,
            pincer_maneuver
        ),
    ]

