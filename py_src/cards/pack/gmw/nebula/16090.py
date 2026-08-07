from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.WhenEnemyActivateAgainstYou') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(targets: Sequence['CardFace']):
            face = player.player_deck.Get(True)[0]
            Faces.RemoveAllFromGame([face], effect)
            Faces.DiscardAll(targets, effect)


        player = message.GetToPlayer()
        ResolveSpecialAbilityOnEachTechniqueAttachmentInPlay(player, effect)
        player.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Remove the top card of your deck from the game to choose and discard 1 of those attachments",
                action
            ).SetTarget(Attachment, trait="TECHNIQUE", canbe_discard=True)
        )


    return [
        TheFirstTechniqueAttachmentEachPlayerRevealedEachRoundGainsSurge(),
        AbilityFactory.WhenEnemyActivateAgainstYou(
            AbilityType.ForcedInterrupt,
            "This",
            nebula
        ),
    ]

