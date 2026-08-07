from cards.pack import *

def PlaceThisCardInInvocationDeckDiscardPile(face: 'CardFace', effect: 'Effect'):
    initiator = effect.GetInitiator()
    Faces.MoveAllTo([face], initiator.additional_discard_pile, effect)
    Message.AfterCardsMoved_Text([face])
    Faces.DiscardAll

