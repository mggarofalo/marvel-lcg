from . import *

# * Elektra

def GetAbilities() -> Sequence['Ability']:

    def elektra(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.AfterYouPlayThisFromHand(
            AbilityType.Response,
            elektra,
        ).SetCost(Cost("R"))
        .SetTarget(Enemy),
    ]

