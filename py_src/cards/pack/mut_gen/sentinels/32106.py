from . import *

# Sentinel Mark VI

def GetAbilities() -> Sequence['Ability']:

    def sentinel_mark_vi_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        def action(player: 'Player'):
            if player.GetIdentity().GetInventoryDeck().FindCard(name="Targeted for Elimination"):
                player.DealEncounterCard(this, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            sentinel_mark_vi_boost
        ),
    ]

