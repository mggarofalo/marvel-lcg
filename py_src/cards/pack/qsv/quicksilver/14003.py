from . import *

# Always Be Running

def GetAbilities() -> Sequence['Ability']:

    def always_be_running(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            always_be_running,
        ).SetPlay()
        .SetTarget(name="Quicksilver", canbe_ready=True)
    ]

