from . import *

# Rapid Deployment

def GetAbilities() -> Sequence['Ability']:

    def rapid_deployment(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)
        if GetResourceGeneratedBySyncRatio(effect):
            this.RemoveThreatFromSchemes(effect.targets2, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            rapid_deployment
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Scheme2),
    ]

