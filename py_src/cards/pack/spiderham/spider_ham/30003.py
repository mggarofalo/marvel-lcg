from . import *

# Ham It Up

def GetAbilities() -> Sequence['Ability']:

    def ham_it_up(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetIdentity().GetCounters('toon')
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ham_it_up,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetIdentity().GetCounters('toon') > 0,
            ]
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

