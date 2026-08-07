from cards.pack import *

DEATHGLOW = "Death-Glow"

def DeathGlowIsSetAside(effect: 'Effect'):
    face = FindDeathGlow(effect)
    if face:
        return face.card.area.flags.is_set_aside
    return False
    # initiator = effect.GetInitiator()
    # # face = initiator.set_aside_deck.FindCard(name=DEATHGLOW, card_type=Upgrade)
    # face = initiator.additional_deck.FindCard(name=DEATHGLOW, card_type=Upgrade)
    # return face != None

def FindDeathGlow(effect: 'Effect') -> 'Upgrade|None':
    initiator = effect.GetInitiator()
    face = initiator.set_aside_deck.FindCard(name=DEATHGLOW, card_type=Upgrade)
    if face:
        return face
    face = Worlds.FindCardOnField(
        effect,
        name=DEATHGLOW,
        card_type=Upgrade
    )
    if face:
        return face
    return None

# def UnitWithDeathGlow(unit: 'Unit2'):
#     faces = unit.GetUpgradeDeck().Get()
#     return Faces.FindCard(faces, name=DEATHGLOW, card_type=Upgrade) != None

HAS_DEATH_GLOW_FINDER = CardFinder(
    has_attach=CardFinder(name=DEATHGLOW, card_type=Upgrade),
    card_type=Enemy
)

