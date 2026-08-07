from . import *

# Manipulated M.U.S.I.C.

def GetAbilities() -> Sequence['Ability']:

    def manipulated_music_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        Find.FindAndPutIntoPlay(
            effect,
            player,
            name="M.U.S.I.C.",
            card_type=Minion
        )

    def manipulated_music(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        face = Worlds.FindCardOnField(
            effect,
            name="M.U.S.I.C.",
        )
        if face:
            Faces.DiscardAll([face], effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            manipulated_music_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            manipulated_music,
        ),
    ]

