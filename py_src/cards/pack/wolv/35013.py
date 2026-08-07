from . import *

# * Psylocke

def GetAbilities() -> Sequence['Ability']:

    def psylocke(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)
        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(
            2, 'psionic'
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.Interrupt,
            "This",
            Enemy,
            psylocke,
        ).SetCostFunc(CostFunc.Counter("This", 1, 'psionic'))
        .SetTarget("Targets", range="All")
    ]

