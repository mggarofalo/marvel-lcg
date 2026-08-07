from . import *

# Agile Flight

def GetAbilities() -> Sequence['Ability']:

    def agile_flight(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemesTotal(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            agile_flight,
        ).SetPlay(only_if_your_identity_has_trait="AERIAL").SetLabel('thwart')
        .SetTarget(Scheme2,
            range=(1, 5),
            repeat_rules="Threat")
    ]

