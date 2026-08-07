from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        ResolveSpecialAbilityOnEachTechniqueAttachmentInPlay(player, effect)
        Faces.DiscardAll(GetInPlayTechniqueAttachment(effect), effect)


    return [
        AbilityFactory.FirstCardPlayerRevealsGain(
            CardFinder2("TECHNIQUE", Attachment),
            "AnyPlayer",
            each_phase_round="Round",
            gain_surge=1
        ),
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            nebula
        ),
    ]

