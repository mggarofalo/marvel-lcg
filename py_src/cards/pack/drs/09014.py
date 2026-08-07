from . import *

# * Iron Fist

def GetAbilities() -> Sequence['Ability']:

    def iron_fist(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)
        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.ThisEnterPlayWithCounters(2, 'mystic'),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.Interrupt,
            "This",
            Enemy,
            iron_fist,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'mystic'))
       .SetTarget("Targets", range="All")
    ]

