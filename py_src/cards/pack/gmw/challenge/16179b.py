from . import *

# Gallery of Splendor

def GetAbilities() -> Sequence['Ability']:

    def gallery_of_splendor(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            faces = player.GetRandomHandCards(1)
            PutCardIntoTheCollection(faces, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.ExpertModeOnly(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            gallery_of_splendor,
        ),
    ]

