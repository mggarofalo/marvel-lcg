from . import *

# Just Passing Through

def GetAbilities() -> Sequence['Ability']:

    def just_passing_through(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            just_passing_through
        ).SetPlay(only_if_you_are_in_additional_from="Intangible").SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetIgnoreKeyword("Patrol", "Crisis"),
    ]

