from . import *

# "I am Groot"

def GetAbilities() -> Sequence['Ability']:

    def i_am_groot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetIdentity().GetCounters('growth')
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            i_am_groot
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

