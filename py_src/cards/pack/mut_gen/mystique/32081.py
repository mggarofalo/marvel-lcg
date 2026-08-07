from . import *

# Metamorphic Mayhem

def GetAbilities() -> Sequence['Ability']:

    def metamorphic_mayhem(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        faces = Worlds.GetEncounterDiscardPile(effect).FindCards(trait="SHAPESHIFTER")
        Faces.ShuffleAllTo(faces, player.player_deck, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            metamorphic_mayhem,
            has_defeating_player=True
        ),
    ]

