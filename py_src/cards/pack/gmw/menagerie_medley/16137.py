from . import *

# Starshark

def GetAbilities() -> Sequence['Ability']:

    def starshark_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            indirect_damage=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            starshark_boost
        ),
    ]

