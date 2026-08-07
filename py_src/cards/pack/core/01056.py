from . import *

# Tac Team

def GetAbilities() -> Sequence['Ability']:

    def tac_team(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        unit = effect.targets[0].CastTo(Unit2)
        this.DealDamage([unit], 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            tac_team
        ).SetTarget(Enemy)
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'attack'))
    ]
