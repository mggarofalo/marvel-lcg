from . import *

# The Missing Milano

def GetAbilities() -> Sequence['Ability']:

    def the_missing_milano(effect: 'Effect', message: 'Message.AfterCardRemovedToken') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        this.Advance(None, effect)


    return [
        AbilityFactory.WhenLastThreatRemoveFromThisScheme(
            AbilityType.ForcedInterrupt,
            the_missing_milano
        ),
    ]

