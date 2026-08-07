from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        ResolveSpecialAbilityOnEachTechniqueAttachmentInPlay(player, effect)
        faces = GetInPlayTechniqueAttachment(effect)
        player.AskDiscardFace(faces, effect)


    return [
        TheFirstTechniqueAttachmentEachPlayerRevealedEachRoundGainsSurge(),
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            nebula
        ),
    ]

