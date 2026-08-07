from . import *

# * Wasp

def GetAbilities() -> Sequence['Ability']:

    def wasp(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit|Message.AfterUnitDefeatedScheme') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "This",
            Minion,
            wasp
        ).SetTarget(Villain)
        .SetName("Small but Mighty"),
        AbilityFactory.AfterUnitDefeatedScheme(
            AbilityType.Response,
            "This",
            SchemeSide2,
            wasp
        ).SetTarget(Villain)
        .SetName("Small but Mighty"),
    ]

