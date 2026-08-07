from cards.pack import *

def PlaceEnergyCounterOnEnergyChannel(size: int, effect: 'Effect') -> 'CanPlaceCounter|None':
    face = Worlds.FindCardOnField(
        effect,
        name="Energy Channel",
        card_type=CanPlaceCounter
    )
    if face:
        Faces.PlaceCountersOn([face], size, 'energy', effect)
        return face
    return None

def FindCaptainMarvel(effect: 'Effect') -> 'Villain|None':
    return Worlds.FindCardOnField(
        effect,
        name="Captain Marvel",
        card_type=Enemy
    )

