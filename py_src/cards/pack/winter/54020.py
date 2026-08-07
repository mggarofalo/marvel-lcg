from . import *

# S.H.I.E.L.D. Sidearm

def GetAbilities() -> Sequence['Ability']:

    def shield_sidearm(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            CardFinder2("S.H.I.E.L.D", Unit2)
        ),
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "AttachedCharacter",
            shield_sidearm,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'ammo'))
        .SetTarget(Enemy),
    ]

