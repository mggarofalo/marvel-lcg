from . import *

# Command Center

def GetAbilities() -> Sequence['Ability']:

    def command_center(effect: 'Effect', message: 'Message.AfterUnitDefeatedScheme') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    return [
        AbilityFactory.AfterUnitDefeatedScheme(
            AbilityType.Response,
            Ally,
            SchemeSide2,
            command_center,
            is_from_thwart=True
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

