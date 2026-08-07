from . import *

# * X-Mansion

def GetAbilities() -> Sequence['Ability']:

    def x_mansion(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            x_mansion
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Unit2, traits=["X-MEN", "MUTANT"], canbe_heal=True)
        .AnyPlayerCanDoThis(player_alter_ego_has_trait="MUTANT"),
    ]

