from . import *

# * White Tiger: Angela Del Toro

def GetAbilities() -> Sequence['Ability']:

    def white_tiger(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            white_tiger,
        ).SetCost(Cost("B"))
        .SetTarget(Scheme2),
    ]

