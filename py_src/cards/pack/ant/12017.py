from . import *

# Power Gloves

def GetAbilities() -> Sequence['Ability']:

    def power_gloves(effect: 'Effect', message: 'Message.AfterUnitAttackEnd|Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("AVENGER", Ally)
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.Response,
            "AttachedAlly",
            power_gloves
        ).SetTarget(Enemy),
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "AttachedAlly",
            power_gloves
        ).SetTarget(Enemy),
    ]

