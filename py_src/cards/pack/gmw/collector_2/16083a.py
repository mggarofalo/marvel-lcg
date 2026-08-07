from . import *

# Lost in the Museum

def GetAbilities() -> Sequence['Ability']:

    def lost_in_the_museum_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        player = message.GetToPlayer()
        face = Search.SearchForCard(
            effect,
            player,
            faces=GetShipCommand(effect).deck.Get(True),
            name="Milano",
            card_type=Support
        )
        if face:
            face.PutIntoPlay(first_player, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            lost_in_the_museum_revealed
        ),
    ]

