from . import *

# * Bombshell

def GetAbilities() -> Sequence['Ability']:

    def bombshell(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.property.divide_damage_among_each_character_attacked_player_control = True

    def bombshell_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        units: List['Unit2'] = []
        for player in Worlds.GetPlayers(effect):
            damage_messages = this.DealIndirectDamage(player.GetIdentity(), 1, effect)
            for damage_message in damage_messages:
                units.append(damage_message.who_took_damage)

        Faces.ExhaustAll(units, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.NonKeyword,
            "This",
            bombshell,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            bombshell_boost
        ),
    ]

