from cards.pack import *

def XMansionIsInPlay(effect: 'Effect'):
    return Worlds.FindCardOnField(
        effect,
        name="X-Mansion"
    ) != None

