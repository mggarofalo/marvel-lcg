from . import *

# Snowy Hillside

def GetAbilities() -> Sequence['Ability']:

    def snowy_hillside(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme and scheme.GetCounters('delay') >= 5:
            value = 2
        else:
            value = 1
        if scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)


    return [
        AfterAbsorbingManMakesUndefendedAttack(snowy_hillside),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]


