from . import *

# * Excelsior

def GetAbilities() -> Sequence['Ability']:

    def excelsior(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            "YourHero",
            excelsior
        ).SetCost(Cost("Y"))
        .SetTarget(Enemy),
    ]

