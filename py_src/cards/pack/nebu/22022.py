from . import *

# Daughters of Thanos

def GetAbilities() -> Sequence['Ability']:

    def daughters_of_thanos(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            daughters_of_thanos
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp"),
    ]

