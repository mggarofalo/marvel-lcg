from . import *

# Fly Over

def GetAbilities() -> Sequence['Ability']:

    def fly_over(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        counter = 1
        def action():
            nonlocal counter
            counter = 2

        this.RemoveThreatFromSchemes(effect.targets, 3, effect, if_this_remove_last_threat=action)

        Faces.PlaceCountersOn([initiator.GetIdentity()], counter, 'progress', effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            fly_over
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

