from . import *

# Med Team

def GetAbilities() -> Sequence['Ability']:

    def med_team(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            med_team
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'medical'))
        .SetTarget(Friend, canbe_heal=True)
    ]

