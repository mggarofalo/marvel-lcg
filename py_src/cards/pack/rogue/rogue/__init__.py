from cards.pack import *

def FindTouched(effect: 'Effect') -> 'Upgrade|None':
    return Find.Find(
        effect,
        name="Touched",
        card_type=Upgrade
    )

def GetTouched(effect: 'Effect', find_any_where: bool=False) -> 'Upgrade|None':
    if find_any_where:
        initiator = effect.GetInitiator()
        # touched = player.set_aside_deck.FindCard(
        #     name="Touched",
        #     card_type=Upgrade
        # )
        # if touched:
        #     return touched
        touched = Search.SearchForCard(
            effect,
            initiator,
            include_player_discard_pile=True,
            include_player_deck=True,
            include_player_hand_cards=True,
            include_player_area=True,
            include_set_aside=True,
            include_in_play=True,
            include_encounter_deck=True,
            include_encounter_discard_pile=True,
            include_removed_area=True,
            include_victory_display=True,
            name="Touched",
            card_type=Upgrade
        )
        return touched
    else:
        touched = Worlds.FindCardOnField(
            effect,
            name="Touched",
            card_type=Upgrade
        )
        return touched

def TouchedAttachedTo(effect: 'Effect', card_type: Type['CardFace']) -> bool:
    return IfTouchedIsAttachTo(effect, card_type) != None

def IfTouchedIsAttachTo(effect: 'Effect', card_type: Type['CardFace']) -> CardFace|None:
    touched = Find.Find(
        effect,
        name="Touched",
        card_type=Upgrade
    )
    if not touched:
        return None
    if touched.attached_face == None:
        return None
    face = touched.GetBindFace()
    if card_type.IsType(face):
        return face
    return None

