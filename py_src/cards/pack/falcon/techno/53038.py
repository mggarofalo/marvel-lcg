from . import *

# * Fixer

def GetAbilities() -> Sequence['Ability']:

    def fixer(effect: 'Effect', message: 'Message.WhenCardWouldAttachTo') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.upgrade.refers_to_the_villain_refers_to_attached = None
        message.AttachToInstead(this, effect)


    return [
        Ability(
            AbilityType.NonKeyword,
            Message.CheckIfRefersToTheVillainRefersToAttached,
            [
                lambda effect, message:
                    Attachment.IsType(message.which_face) and \
                    effect.this == message.which_face.GetBindFace()
            ],
            lambda effect, message:
                message.SetRefersTo(effect),
        ),
        AbilityFactory.WhenCardWouldAttachTo(
            AbilityType.ForcedInterrupt,
            CardFinder(trait="TECH", card_type=Attachment, is_scenario_specific=False),
            Villain,
            fixer
        ),
    ]

