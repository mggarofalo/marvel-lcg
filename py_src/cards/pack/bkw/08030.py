from . import *

# Counterattack

def GetAbilities() -> Sequence['Ability']:

    def counterattack(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, message.took_damage, effect)


    return [
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.HeroResponse,
            "You",
            counterattack,
            is_from_attack=True,
            who_dealt_damage=Enemy,
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("Attacker"),
    ]

