from . import *

# Fighting Zemo

def GetAbilities() -> Sequence['Ability']:

    def fighting_zemo_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Baron Zemo"
        )
        assert villain

        # Flip the mole to its attachment side and attach it to Baron Zemo.
        evidence = Worlds.FindCardOnField(
            effect,
            card_type=Evidence
        )
        assert evidence

        mole = evidence.GetBindFace()
        mole.FlipTo(effect, card_type=Attachment)
        mole.card.face.CastTo(Attachment).AttachTo2(villain, effect)

        attachments = Worlds.FindCardsOnField(effect, trait="BOARD MEMBER", card_type=Attachment)
        size = Worlds.GetPlayerNumIcon(effect) * Faces.GetTotalCounters(attachments, 'secret')
        this.PlaceThreatOnSchemes([this], size, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            fighting_zemo_revealed
        ),
    ]

