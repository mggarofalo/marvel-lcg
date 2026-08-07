from cards.pack import *

def GetSetAsideFrostbite(player: 'Player') -> 'Upgrade|None':
    face = player.set_aside_deck.FindCard(name="Frostbite", card_type=Upgrade)
    return face

