from . import *

# Sentinel

def GetAbilities() -> Sequence['Ability']:

    def sentinel_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)
        scheme = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Abduction Protocols",
            card_type=SchemeSide2
        )
        if scheme:
            scheme.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sentinel_revealed
        ),
    ]

