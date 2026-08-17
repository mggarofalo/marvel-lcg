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
        # "deal 1 damage to each enemy" -- every enemy, not a choice of them.
        # Found by the MARVEL-129 grep for `(1, "All")` against printed "each":
        # this is the second card of the two that spelling was wrong on.
        .SetTarget(Enemy, range="All")
    ]

