from . import *

# * Quicksilver: Pietro Maximoff

def GetAbilities() -> Sequence['Ability']:

    def quicksilver(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            quicksilver
        ).SetTarget("This", canbe_ready=True)
        .LimitOncePerPhase(),
    ]

