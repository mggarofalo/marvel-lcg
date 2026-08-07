from . import *

# Test Subjects

def GetAbilities() -> Sequence['Ability']:

    def test_subjects(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Minion,
        )

        if face:
            face.Reveal(first_player, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            test_subjects
        )
    ]

