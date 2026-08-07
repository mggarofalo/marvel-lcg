from . import *

# Soup Kitchen

def GetAbilities() -> Sequence['Ability']:

    def soup_kitchen(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        value = effect.targets[0].CastTo(HasRecover).printed_recovery
        this.HealthUnits(effect.targets, value, effect)

        Worlds.UpdateNextCardPlayCost(
            "Any",
            -2,
            effect,
            finder=CardFinder(card_type=Ally|Support),
            in_this="Phase"
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            soup_kitchen
        ).SetCostFunc(CostFunc.Exhaust(name="Sam Wilson"))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Sam Wilson", canbe_heal=True),
    ]

