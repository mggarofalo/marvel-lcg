from . import *

# * War Machine

def GetAbilities() -> Sequence['Ability']:

    def war_machine(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            war_machine,
        )
        # .SetDesc("Exhaust War Machine and deal 2 damage to him → deal 1 damage to each enemy")
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealDamage(2, "This"))
        .SetTarget(Enemy, range=(1, "All"))
    ]

