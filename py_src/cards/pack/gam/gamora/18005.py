from . import *

# Set the Pace

def GetAbilities() -> Sequence['Ability']:

    def set_the_pace(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            set_the_pace
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

