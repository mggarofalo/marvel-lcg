from cards.pack import *

def GetWeatherDeck(effect: 'Effect'):
    return effect.GetInitiator().set_aside_deck.Get()

def FindYourWeatherSupport(player: 'Player'):
    return player.supports.FindCard(trait="WEATHER", card_type=Support)

def SwapYourWeatherSupport(player: 'Player', faces: List['CardFace'], resolve_special: bool, by_effect: 'Effect'):
    support = FindYourWeatherSupport(player)
    if support:
        Faces.MoveAllTo([support], player.set_aside_deck, by_effect)
        Faces.FlipAllTo(player.set_aside_deck.GetAll(), True, by_effect)
        for face in faces:
            face.CastTo(Support).PutIntoPlay(player, by_effect)
            if resolve_special:
                face.ResolveSpecialAbility(player, by_effect)

