from cards.pack import *

def GetExperimentalWeapons(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "ExperimentalWeapons")

def RevealExperimentalWeaponsDeckTopCard(effect: 'Effect'):
    face = GetExperimentalWeapons(effect).GetTopCard()
    if face:
        face.Reveal(None, effect)

