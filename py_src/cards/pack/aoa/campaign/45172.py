from . import *

# * Destiny: Irene Adler

def GetAbilities() -> Sequence['Ability']:

    def destiny(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterCardEnterHand(
            AbilityType.Response,
            "This",
            destiny
        ).SetTarget(MainScheme),
    ]

