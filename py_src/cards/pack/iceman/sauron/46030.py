from . import *

# Sauron Lives!

def GetAbilities() -> Sequence['Ability']:

    def sauron_lives(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Sauron"
        )
        if face:
            player.DealEncounterCard(face, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            sauron_lives,
            has_defeating_player=True
        ),
    ]

