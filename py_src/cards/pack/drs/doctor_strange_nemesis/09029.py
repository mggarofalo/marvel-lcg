from . import *

# Open the Dark Dimension

def GetAbilities() -> Sequence['Ability']:

    def open_the_dark_dimension_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        # Place the top card of the INVOCATION deck facedown under this scheme.
        player = Worlds.FindPlayerByName("Stephen Strange", effect)
        if player:
            face = player.additional_deck.GetTop()
            if face:
                this.PlaceCardHere(face, False, effect)

    def open_the_dark_dimension_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        # Shuffle the INVOCATION card under here into the INVOCATION deck
        player = Worlds.FindPlayerByName("Stephen Strange", effect)
        if player:
            face = player.additional_deck.GetTop()
            if face:
                face.FlipTo(effect, face_up=False)
            Faces.ShuffleAllTo(this.GetPlacedCardArea().Get(), player.additional_deck, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            open_the_dark_dimension_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            open_the_dark_dimension_defeated
        )
    ]

