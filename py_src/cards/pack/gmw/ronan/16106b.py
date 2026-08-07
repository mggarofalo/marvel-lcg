from . import *

# Interception Imminent

def GetAbilities() -> Sequence['Ability']:

    def interception_imminent(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.RemoveThreatFromSchemes([this], 3, effect)


    return [
        ExhaustTheMilano(
            None,
            interception_imminent
        ).SetTarget("This", has_threat=True),
    ]

