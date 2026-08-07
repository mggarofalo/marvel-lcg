from . import *

# * Nova: Sam Alexander

def GetAbilities() -> Sequence['Ability']:

    def nova(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.Interrupt,
            Enemy,
            "You",
            nova
        ).SetCost(Cost("Y"))
        .SetTarget("Attacker"),
    ]

