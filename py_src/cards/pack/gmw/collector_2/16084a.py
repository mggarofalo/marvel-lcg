from . import *

# The Great Escape

def GetAbilities() -> Sequence['Ability']:

    def the_great_escape_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        face = GetLibraryLabyrinth(effect)
        face.card.Flip(effect)

        this.PlaceAccelerationToken(1, effect)
        # faces: List[CardFace] = []
        # for face in world.GetSetAsideAreaCards():
        #     if face.paper.IsFromSet("Ship Command"):
        #         faces.append(face)

        encounter_deck = Worlds.GetEncounterDeck(effect)
        Faces.ShuffleAllTo(GetShipCommand(effect).deck.Get(), encounter_deck, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_great_escape_revealed
        ),
    ]

