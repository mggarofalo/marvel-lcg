from . import *

# * Warren Worthington III

def GetAbilities() -> Sequence['Ability']:

    def warren_worthington_iii(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            warren_worthington_iii
        ).SetName("Regrowth")
        .SetTarget("This", canbe_heal=True)
        .LimitOncePerRound(),
    ]

