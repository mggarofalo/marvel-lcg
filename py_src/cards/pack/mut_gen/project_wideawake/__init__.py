from cards.pack import *

def PlaceDeckTopCardFacedownUnderOperationZeroTolerance(player: 'Player', effect: 'Effect'):
    face = player.player_deck.Get(True)[0]
    PlaceCardFacedownUnderOperationZeroTolerance(face, effect)

def PlaceCardFacedownUnderOperationZeroTolerance(face: 'CardFace', effect: 'Effect'):
    scheme = Worlds.FindCardOnField(
        effect,
        name="Operation Zero Tolerance",
        card_type=SchemeSide2
    )
    if scheme:
        scheme.PlaceCardHere(face, False, effect)

