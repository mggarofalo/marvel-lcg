from . import *

# * Bug

def GetAbilities() -> Sequence['Ability']:

    def bug(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitMakeBasicAttack(
            AbilityType.HeroResponse,
            "YourHero",
            bug,
        ).SetTarget("This", canbe_heal=True)
    ]

