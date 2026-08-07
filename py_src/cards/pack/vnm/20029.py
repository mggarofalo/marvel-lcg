from . import *

# Crew Quarters

def GetAbilities() -> Sequence['Ability']:

    def crew_quarters(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
            under_any_players_control=True
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            crew_quarters
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(AlterEgo)
    ]

