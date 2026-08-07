from . import *

# Inconspicuous

def GetAbilities() -> Sequence['Ability']:

    def inconspicuous(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemesTotal(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            inconspicuous
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2,
            range=(1, 3),
            repeat_rules="Threat"),
    ]

