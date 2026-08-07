from . import *

# * Marrow: Sarah

def GetAbilities() -> Sequence['Ability']:

    def marrow(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_one_of_traits=["X-FORCE", "X-MEN"]),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            marrow
        ).SetTarget(Enemy),
    ]

