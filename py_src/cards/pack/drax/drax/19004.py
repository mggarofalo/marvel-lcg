from . import *

# Intimidation

def GetAbilities() -> Sequence['Ability']:

    def intimidation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetHero().attack
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            intimidation
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

