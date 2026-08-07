from . import *

# Breaking and Entering

def GetAbilities() -> Sequence['Ability']:

    def breaking_and_entering(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            breaking_and_entering
        ).SetPlay(only_if_your_identity_has_one_of_traits=["SPY", "THIEF"]).SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

