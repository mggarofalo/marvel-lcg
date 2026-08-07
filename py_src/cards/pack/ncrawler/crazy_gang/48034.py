from . import *

# * Queen of Hearts

def GetAbilities() -> Sequence['Ability']:

    def queen_of_hearts_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        finder = CardFinder(
            name="The Crazy Gang",
            card_type=SchemeSide2,
        )

        face = Worlds.FindCardOnField(
            effect,
            finder
        )
        if face:
            player.DealEncounterCards(1, effect)
        else:
            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                finder=finder,
            )
            if face:
                face.Reveal(player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            queen_of_hearts_revealed
        ),
    ]

