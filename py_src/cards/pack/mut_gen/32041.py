from . import *

# * Wolverine: Logan

def GetAbilities() -> Sequence['Ability']:

    def wolverine(effect: 'Effect', message: 'Message.AfterPlayerTurnBegin') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterPlayerTurnBegin(
            AbilityType.Response,
            "You",
            wolverine
        ).SetTarget("This"),
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        )
    ]

