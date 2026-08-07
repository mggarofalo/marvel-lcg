from cards.pack import *

def SearchEncounterDeckForMinionAndAttachTo(effect: 'Effect', player: 'Player') -> None:
    this = effect.this.CastTo(Attachment)
    Unused(this)

    face = Search.EncounterCard(
        effect,
        player,
        include_discard_pile=True,
        card_type=Minion,
    )
    if face:
        face.PutIntoPlay(player, effect)
        this.AttachTo2(face, effect)

