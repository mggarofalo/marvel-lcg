from . import *

# Kree Command Ship

def GetAbilities() -> Sequence['Ability']:

    def kree_command_ship(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        message.CancelWhenRevealedEffect(effect)


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.FirstPlayerInterrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            kree_command_ship,
            from_encounter=True,
        ).SetCostFunc(CostFunc.Exhaust(name="Milano"))
        .SetCost(Cost("1"))
        .SetTarget("Trigger"),
    ]
