from . import *

# * Crossfire

def GetAbilities() -> Sequence['Ability']:

    def crossfire_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        message.would_atk_message.GainPiercing(effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            crossfire_boost,
            during_attack=True,
        )
    ]

