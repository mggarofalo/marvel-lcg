from . import *

# * Gambit's Staff

def GetAbilities() -> Sequence['Ability']:

    def gambits_staff(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Enemy,
            gambits_staff
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Attacker"),
    ]

