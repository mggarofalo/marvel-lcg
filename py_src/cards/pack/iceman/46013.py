from . import *

# * Glob: Robert Herman

def GetAbilities() -> Sequence['Ability']:

    def glob(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_trait="X-MEN"),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            glob
        ).SetTarget(Enemy, with_attach=Upgrade),
    ]

