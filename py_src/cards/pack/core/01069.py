from . import *

# Get Ready

def GetAbilities() -> Sequence['Ability']:

    def get_ready(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            get_ready
        ).SetPlay()
        .SetTarget(Ally, canbe_ready=True)
    ]

