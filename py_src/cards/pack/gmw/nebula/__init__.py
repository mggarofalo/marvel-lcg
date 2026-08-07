from cards.pack import *
from cards.pack.gmw import *

def FindNebulaShip(effect: 'Effect'):
    face = Worlds.FindCardOnField(
        effect,
        name="Nebula's Ship",
        card_type=Environment
    )
    return face

def TheFirstTechniqueAttachmentEachPlayerRevealedEachRoundGainsSurge() -> 'Ability':
    # See also "17026"
    return AbilityFactory.WhenCardRevealed(
        AbilityType.NonKeyword,
        None,
        lambda effect, message:
            message.trigger.CastTo(Attachment).GainSurge(+1, effect),
        conditions=[
            lambda effect, message:
                Attachment.IsType(message.trigger) and \
                message.trigger.HasTrait("TECHNIQUE")
        ],
    ).LimitOncePerRoundPerPlayer()

def GetInPlayTechniqueAttachment(effect: 'Effect'):
    return Worlds.FindCardsOnField(
        effect,
        trait="TECHNIQUE",
        card_type=Attachment
    )

def ResolveSpecialAbilityOnEachTechniqueAttachmentInPlay(player: 'Player', effect: 'Effect'):
    faces = GetInPlayTechniqueAttachment(effect)
    player.ResolveSpecialAbility(faces, effect)

def AfterThisActivationEndsAttachThisCardToNebulaAndResolveItsSpecialAbility():

    def boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        def action():
            villain = Worlds.FindVillain(effect)
            if villain:
                this.AttachTo2(villain, effect)
                this.ResolveSpecialAbility(player, effect)

        message.AfterThisActivation(effect, action)


    return AbilityFactory.WhenCardBecomeBoost(
        "This",
        boost
    )
