from . import *

# Cerebral Erasure

def GetAbilities() -> Sequence['Ability']:

    def cerebral_erasure_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        faces = player.GetControlCardsByType(upgrade=True, support=True)
        face = player.AskChooseFace(faces, effect)
        if face:
            Faces.ReturnToHand([face], "Owner", effect)

    def cerebral_erasure(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        faces = player.GetControlCardsByType(upgrade=True, support=True)
        face = player.AskChooseFace(faces, effect)
        if face:
            Faces.ReturnToHand([face], "Owner", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            cerebral_erasure_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            cerebral_erasure,
            has_defeating_player=True
        ),
    ]

