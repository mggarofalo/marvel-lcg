from . import *

# Pyro 4.0

def GetAbilities() -> Sequence['Ability']:

    def pyro_40(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.property.against_player
        if player:
            identity = player.GetIdentity()
            identity.TakeIndirectDamage(this, 2, effect)

    def pyro_40_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        identity.TakeIndirectDamage(this, 2, effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "This",
            pyro_40
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            pyro_40_boost
        ),
    ]

