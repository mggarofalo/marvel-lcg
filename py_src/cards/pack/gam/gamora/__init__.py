from cards.pack import *

def HavePlayedEventThisTurn(player: 'Player', trait: 'CardFace.TRAITS') -> bool:
    for face in player.stat.this_turn_played_cards:
        if face.HasTrait(trait):
            return True
    return False

