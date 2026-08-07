from cards.pack import *

def FindFinesse(by_effect: 'Effect') -> 'Attachment':
    face = Worlds.FindCardOnField(by_effect, name="Finesse", card_type=Attachment)
    assert face
    return face

def PlaceSkillCounterOnFinesse(size: int, by_effect: 'Effect'):
    face = FindFinesse(by_effect)
    Faces.PlaceCountersOn([face], 1, 'skill', by_effect)

