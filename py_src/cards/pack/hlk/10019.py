from . import *

# To the Rescue!

def GetAbilities() -> Sequence['Ability']:

    def to_the_rescue(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            to_the_rescue
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

