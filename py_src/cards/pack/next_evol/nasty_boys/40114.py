from . import *

# * Ramrod

def GetAbilities() -> Sequence['Ability']:

    def ramrod_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.would_atk_message.GainPiercing(effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ramrod_boost,
            during_attack=True,
            attacker=Villain
        ),
    ]

