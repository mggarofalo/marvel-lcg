from . import *

# * Jarnbjorn

def GetAbilities() -> Sequence['Ability']:

    def jarnbjorn(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.Response,
            "YourHero",
            Enemy,
            jarnbjorn,
        ).SetCost(Cost("R")).SetTarget(Enemy)
    ]

