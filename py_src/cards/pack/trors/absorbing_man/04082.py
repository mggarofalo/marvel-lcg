from . import *

# Rocky Outcrop

def GetAbilities() -> Sequence['Ability']:

    def rocky_outcrop(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme and scheme.GetCounters('delay') >= 5:
            value = 2
        else:
            value = 1
        this.HealthUnits([GetAbsorbingMan(effect)], value, effect)

    return [
        AfterAbsorbingManMakesUndefendedAttack(rocky_outcrop),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]

