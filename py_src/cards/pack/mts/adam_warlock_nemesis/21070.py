from . import *

# Cosmic Inquisition

def GetAbilities() -> Sequence['Ability']:

    def cosmic_inquisition_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        scheme = Worlds.FindCardsOnField(
            effect,
            name="Universal Church of Truth",
            card_type=SchemeSide2
        )
        if scheme:
            player.DiscardDeckTopCards(10, effect)
        else:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                include_set_aside=True,
                name="Universal Church of Truth",
                card_type=SchemeSide2,
            )
            if face:
                face.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            cosmic_inquisition_revealed
        ),
    ]

