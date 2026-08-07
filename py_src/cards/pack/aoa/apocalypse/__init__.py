from cards.pack import *

def RevealRandomSetasidePRELATEMinion(player: 'Player', effect: 'Effect'):
    faces = Worlds.AsideDeck(effect).FindCards(trait="PRELATE", card_type=Minion)
    face = Rand.RandomChoice(faces, effect)
    face.Reveal(player, effect)

