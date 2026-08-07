from . import *

# * Squirrel Girl: Doreen Green

def GetAbilities() -> Sequence['Ability']:

    def squirrel_girl(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            squirrel_girl
        ).SetTarget(Enemy, range="All")
    ]

