from . import *

# Gatekeeper

def GetAbilities() -> Sequence['Ability']:

    def gatekeeper(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 4, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=2,
            patrol=True,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.Interrupt,
            "AttachedMinion",
            gatekeeper
        ).SetTarget(MainScheme),
    ]

