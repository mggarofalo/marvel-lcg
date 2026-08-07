from . import *

# The Titan's Throne

def GetAbilities() -> Sequence['Ability']:

    def the_titans_throne(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)

        faces = Worlds.FindCardsOnField(effect, trait="INFINITY STONE")
        player.AskDiscardFace(faces, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_titans_throne,
        ),
    ]

