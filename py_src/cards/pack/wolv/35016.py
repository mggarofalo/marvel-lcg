from . import *

# Warrior Skill

def GetAbilities() -> Sequence['Ability']:

    def warrior_skill(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.DealAdditionalDamage(1, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "YourHero",
            warrior_skill
        ).SetCostFunc(CostFunc.Counter("This", 1, 'warrior')),
    ]

