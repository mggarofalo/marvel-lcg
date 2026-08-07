from . import *

# * Juggernaut

def GetAbilities() -> Sequence['Ability']:

    def juggernaut_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        atk_message = message.would_atk_message
        atk_message.GainOverKill(effect)
        atk_message.GainPiercing(effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            juggernaut_boost,
            during_attack=True,
        ),
    ]

