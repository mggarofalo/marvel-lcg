from . import *

# Ambush

def GetAbilities() -> Sequence['Ability']:

    def ambush(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(SchemeSide2),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.Interrupt,
            "AttachedSideScheme",
            ambush
        ).SetTarget(Minion, non_trait="ELITE"),
    ]

