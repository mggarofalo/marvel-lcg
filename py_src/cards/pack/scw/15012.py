from . import *

# Crisis Averted

def GetAbilities() -> Sequence['Ability']:

    def crisis_averted(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        if effect.GetPaidResources().HasColor("B"):
            ignore_crisis = True
        else:
            ignore_crisis = False

        this.RemoveThreatFromSchemes(effect.targets, 6, effect, ignore_crisis=ignore_crisis)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            crisis_averted,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(MainScheme)
    ]

