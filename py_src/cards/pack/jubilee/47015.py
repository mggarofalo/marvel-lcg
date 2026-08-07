from . import *

# Three Steps Ahead

def GetAbilities() -> Sequence['Ability']:

    def three_steps_ahead(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        size = effect.GetPaidResources().GetResourceIconTypes()
        schemes = effect.targets[:size]
        this.RemoveThreatFromSchemes(schemes, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            three_steps_ahead
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2, range=(0, 4), repeat_rules="Threat"),
    ]

