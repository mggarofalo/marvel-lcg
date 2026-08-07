from . import *

# * Warlock

def GetAbilities() -> Sequence['Ability']:

    def warlock(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            warlock,
        ).SetCost(Cost("B"))
        .SetTarget("This", canbe_heal=True)
    ]

