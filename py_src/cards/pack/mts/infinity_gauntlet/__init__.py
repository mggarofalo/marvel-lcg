from cards.pack import *

def GetInfinityStone(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "InfinityStone")

def PlaceThisCardInTheInfinityStoneDeckDiscardPile(effect: 'Effect'):
    this = effect.this
    Faces.MoveAllTo([this], GetInfinityStone(effect).discard_pile, effect)

