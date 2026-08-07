from . import *

# Protect the Students

def GetAbilities() -> Sequence['Ability']:

    def protect_the_students(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.PlayerCard(
            effect,
            player,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Ally
        )
        if face:
            player.GainCard(face, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            protect_the_students,
            has_defeating_player=True
        ),
    ]

