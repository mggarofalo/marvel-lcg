from cards.pack import *
from cards.pack.gmw import *

LIBRARY_LABYRINTH = "Library Labyrinth"

def GetShipCommand(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "ShipCommand")

def GetLibraryLabyrinth(effect: 'Effect'):
    face = Worlds.FindCardOnField(
        effect,
        name=LIBRARY_LABYRINTH,
        card_type=Environment
    )
    assert face
    return face

