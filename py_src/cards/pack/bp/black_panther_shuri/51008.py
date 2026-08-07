from . import *

# * Queen Ramonda

def GetAbilities() -> Sequence['Ability']:

    def queen_ramonda(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        value = effect.targets[0].CastTo(AlterEgo).recover
        this.HealthUnits(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            queen_ramonda
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(AlterEgo, trait="WAKANDA", canbe_heal=True),
    ]

