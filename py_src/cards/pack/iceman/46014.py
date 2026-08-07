from . import *

# Suppressing Fire

def GetAbilities() -> Sequence['Ability']:

    def suppressing_fire(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(Minion),
        AbilityFactory.WhenUnitAttackAndDefeatUnit(
            AbilityType.HeroInterrupt,
            "You",
            "AttachedMinion",
            suppressing_fire
        ).SetTarget("YourHero", canbe_heal=True),
    ]

