from . import *

# * Tigra

def GetAbilities() -> Sequence['Ability']:

    def tigra(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Ally)

        this.HealthUnits([effect.this], 1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "This",
            Minion,
            tigra,
        ).SetTarget("This", canbe_heal=True)
    ]

