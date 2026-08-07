from . import *

# Cyttorak's Exemplar

def GetAbilities() -> Sequence['Ability']:

    def cyttoraks_exemplar_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect, "Juggernaut")

        face = Worlds.FindCardOnField(
            effect,
            name="Juggernaut Exposed",
            card_type=Attachment
        )
        if face:
            face.card.Flip(effect)
            if villain:
                Faces.PlaceCountersOn([villain], 1, 'momentum', effect)
        else:
            if villain:
                this.PlaceThreatOnSchemes("MainScheme", villain.attack, effect)

    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            incite=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            cyttoraks_exemplar_revealed
        ),
    ]

