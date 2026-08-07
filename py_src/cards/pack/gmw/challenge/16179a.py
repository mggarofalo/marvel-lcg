from . import *

# Gallery of Splendor

def GetAbilities() -> Sequence['Ability']:

    def gallery_of_splendor(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            PutTopCardIntoTheCollection(player, effect)

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.StandardModeOnly(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            gallery_of_splendor,
        ),
    ]

