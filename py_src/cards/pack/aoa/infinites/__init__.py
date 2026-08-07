from cards.pack import *

def GetGenePool(effect: 'Effect') -> 'SchemeSide2|None':
    scheme = Worlds.FindCardOnField(
        effect,
        name="Gene Pool",
        card_type=SchemeSide2
    )
    return scheme

def GetThreatOnGenePool(effect: 'Effect') -> int:
    scheme = GetGenePool(effect)
    if scheme:
        return scheme.threat
    else:
        return 0
