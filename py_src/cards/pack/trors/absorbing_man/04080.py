from . import *

# Dense Forest

def GetAbilities() -> Sequence['Ability']:

    def dense_forest(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = message.GetToPlayer()
        scheme = Worlds.FindMainScheme(this)
        if scheme and scheme.GetCounters('delay') >= 5:
            value = 2
        else:
            value = 1
        player.GetIdentity().TakeIndirectDamage(this, value, effect)


    return [
        AfterAbsorbingManMakesUndefendedAttack(dense_forest),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            PutThisIntoPlay,
        )
    ]


