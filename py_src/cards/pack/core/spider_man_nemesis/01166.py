from . import *

# Highway Robbery

def GetAbilities() -> Sequence['Ability']:

    def highway_robbery_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        def action(player: 'Player'):
            return player.GetRandomHandCards(1)

        faces: List['CardFace'] = Players.ForEachPlayer(effect, action, [])

        this.PlaceCardHere(faces, False, effect)

    def highway_robbery_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        
        faces = this.GetPlacedCardArea().Get(True)
        Faces.ReturnToHand(faces, "Owner", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            highway_robbery_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            highway_robbery_defeated
        )
    ]

