from . import *

# Sneak By

def GetAbilities() -> Sequence['Ability']:

    def sneak_by(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sneak_by
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

