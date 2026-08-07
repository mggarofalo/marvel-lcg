from cards.pack import *

def CreateUltronFacedownDrone(player: Player, size: int, by_effect: 'Effect'):
    Worlds.Enemies.PutYouDeckTopCardAsFacedownMinion(player, "Drone Minion", by_effect, size=size)

