from . import *

# Sub-Orbital Leap

def GetAbilities() -> Sequence['Ability']:

    def sub_orbital_leap(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 3
        if effect.GetPaidResources().OnlyHasColor("R"):
            value = 5

        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sub_orbital_leap
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

