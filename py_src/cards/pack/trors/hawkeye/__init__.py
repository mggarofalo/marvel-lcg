from cards.pack import *

HAWKEYE_BOW_NAME = "Hawkeye's Bow"

def FindOnPlayHawkeyesBow(player: 'Player') -> Upgrade|None:
    face = player.GetIdentity().GetInventoryDeck().FindCard(name=HAWKEYE_BOW_NAME, card_type=Upgrade)
    return face

