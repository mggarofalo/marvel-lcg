from . import *

# * Magneto: Erik Lehnsherr

def GetAbilities() -> Sequence['Ability']:

    def magneto(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits([this], 1, effect)


    return [
        AbilityFactory.FirstPlayerControlThis(),
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            "This",
            CardFinder2("SENTINEL", Minion),
            magneto
        ).SetTarget("This", canbe_heal=True),
    ]

