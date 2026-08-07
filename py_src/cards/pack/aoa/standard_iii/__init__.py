from cards.pack import *

def GetPursuedByThePast(effect: 'Effect') -> 'Environment|None':
    face = Worlds.FindCardOnField(
        effect,
        name="Pursued by the Past",
        card_type=Environment
    )
    return face
