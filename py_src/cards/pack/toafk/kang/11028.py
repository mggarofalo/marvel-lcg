from . import *

# Time-Travel Tactics

def GetAbilities() -> Sequence['Ability']:

    def time_travel_tactics(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        def action(player: 'Player'):
            damage = player.obligations_area.GetSize()
            player.GetIdentity().TakeIndirectDamage(this, damage, effect)

        Players.ForEachPlayer(effect, action)

    def time_travel_tactics_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        player = message.GetToPlayer()
        icon = player.obligations_area.GetSize()
        message.GainBoostIcon(icon, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            time_travel_tactics
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            time_travel_tactics_boost
        )
    ]

