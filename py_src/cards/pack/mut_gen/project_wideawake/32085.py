from . import *

# Sentinel

def GetAbilities() -> Sequence['Ability']:

    def sentinel_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        scheme = Search.EncounterCard(
            effect,
            first_player,
            include_discard_pile=True,
            name="Abduction Protocols",
            card_type=SchemeSide2
        )
        if scheme:
            scheme.Reveal(first_player, effect)

        def action(player: 'Player'):
            if player != first_player:
                player.DealEncounterCards(1, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sentinel_revealed
        ),
    ]

