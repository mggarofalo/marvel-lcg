from . import *

# For Justice!

def GetAbilities() -> Sequence['Ability']:

    def for_justice(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        if effect.GetPaidResources().HasColor("B"):
            value = 4
        else:
            value = 3
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            for_justice
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
    ]

