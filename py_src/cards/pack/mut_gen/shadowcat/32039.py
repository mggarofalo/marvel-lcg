from . import *

# Airwalk

def GetAbilities() -> Sequence['Ability']:

    def airwalk(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        value = 2
        if initiator.form.IsInMassForm("Phased"):
            value = 4
        this.RemoveThreatFromSchemes(effect.targets, value, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            airwalk
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

