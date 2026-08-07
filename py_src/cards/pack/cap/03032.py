from . import *

# Followed

def GetAbilities() -> Sequence['Ability']:

    def followed(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(SchemeSide2),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.Interrupt,
            "AttachedScheme",
            followed
        ).SetTarget(Enemy)
        # Fix "01007" vs "03032"
    ]

