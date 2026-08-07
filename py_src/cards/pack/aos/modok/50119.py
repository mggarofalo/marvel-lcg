from . import *

# Reverse Engineering

def GetAbilities() -> Sequence['Ability']:

    def reverse_engineering_tuck(effect: 'Effect', face: 'CardFace', ui: List['CardFace']) -> int:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        faces = this.GetPlacedCardArea().GetAll()
        ui.extend(faces)
        return FacesCounter.GetPrintedCost(faces)

    def reverse_engineering(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        face = player.AskChooseFace(player.GetControlCardsByType(upgrade=True), effect)
        if not face:
            face = player.player_deck.GetTop()

        if face:
            this.TuckCardUnderHere(face, effect, peek=True)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            MODOK_FINDER
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            reverse_engineering
        ),
        *AbilityFactory.GiveKeywordToAttached(
            get_new_value=reverse_engineering_tuck,
            attack=1,
            scheme=1,
            ex_change_on_event=OnEvent.TuckUnder("Here")
        ),
        AfterMODOKHitPointsAreResetDiscardThisCard()
    ]

