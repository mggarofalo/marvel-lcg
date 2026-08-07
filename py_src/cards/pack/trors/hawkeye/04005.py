from . import *

# Sonic Arrow

def GetAbilities() -> Sequence['Ability']:

    def sonic(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)

        for unit in effect.targets:
            target = unit.CastTo(Unit2)
            if target.IsConfused():
                damage = 5
            else:
                damage = 3
            Faces.GiveStatus([target], "Confused", effect)
            this.DealDamage([target], damage, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sonic,
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            name=HAWKEYE_BOW_NAME,
            from_where=["YouControlCards"])
        )
    ]
