from . import *

# Godslayer

def GetAbilities() -> Sequence['Ability']:

    def godslayer(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            if Unit2.IsType(target):
                target.GainForThisActive(effect, message, attack=2)


    return [
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.HeroInterrupt,
            "YourHero",
            CardFinder(card_type=Enemy, is_unique=True),
            godslayer,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Attacker"),
    ]

