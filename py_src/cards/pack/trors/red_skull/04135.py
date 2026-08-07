from . import *

# Twisted Reality

def GetAbilities() -> Sequence['Ability']:

    def twisted_reality(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        first_player.DealEncounterCards(1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            SchemeSide2
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedSideScheme",
            twisted_reality
        )
    ]

