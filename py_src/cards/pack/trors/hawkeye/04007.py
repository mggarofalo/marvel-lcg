from . import *

# Explosive Arrow

def GetAbilities() -> Sequence['Ability']:

    def electric_arrow(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)

        for unit in effect.targets:
            unit = unit.CastTo(Unit2)
            damage = 3
            if unit.IsStunned():
                damage = 5
            else:
                Faces.GiveStatus([unit], "Stunned", effect)
            this.DealDamage([unit], damage, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            electric_arrow,
        ).SetPlay().SetLabel('attack').SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            name=HAWKEYE_BOW_NAME,
            from_where=["YouControlCards"])
        )
    ]

