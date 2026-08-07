from cards.pack import *

def GetPowerStone(by_effect: 'Effect') -> 'Attachment|None':

    first_player = Worlds.GetFirstPlayer(by_effect)
    face = Search.SearchForCard(
        by_effect,
        first_player,
        include_in_play=True,
        name="Power Stone",
        card_type=Attachment,
    )
    return face

def IsControlPowerStone(unit: 'Unit2|None') -> bool:
    if unit == None:
        return False
    return unit.GetInventoryDeck().FindCardSize(CardFinder(name="Power Stone")) > 0

