from . import *

# Yon-Rogg's Treason

def GetAbilities() -> Sequence['Ability']:

    def yon_roggs_treason(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.hand_cards.FindCards(has_printed_res="Y")
        if faces == []:
            this.GainSurge(1, effect)
        else:
            Faces.DiscardAll(faces, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            yon_roggs_treason
        )
    ]

