from cards.pack import *

def RedSkullGetsATKForEachSideSchemeInPlay():
    return AbilityFactory.ThisGainKeywordForEachFaceInPlay(
        CardFinder(card_type=SchemeSide2),
        attack=1,
    )

def GetSideSchemeDeck(effect: 'Effect'):
    return Worlds.ScenarioDeck(effect, "SideSchemeDeck")

def RevealSideSchemeDeckTopCard(effect: 'Effect'):
    face = GetSideSchemeDeck(effect).GetTopCard()
    if face:
        face.Reveal(None, effect)

