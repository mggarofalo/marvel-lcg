from . import *

# Powerful Enchantments

def GetAbilities() -> Sequence['Ability']:

    return [
        # Ability(
        #     AbilityType.NonKeyword,
        #     Send.CheckEffectCondition,
        #     lambda effect, message:
        #         # This is a hack
        #         # Our version current version can't check the `discard` action
        #         isinstance(message.for_effect.for_message, Send.WhenPlayerInTurn) and \
        #         Attachment.IsType(message.for_effect.this) and \
        #         isinstance(message.for_effect.this.GetAttachedCard().face, Identity|Ally) and \
        #         message.for_effect.initiator.IsType(Player),
        #     lambda effect, message:
        #         message.SetCannot(effect)
        # )
        # It just said cannot discard, it doesn't said you cannot trigger that ability
        AbilityFactory.WhenCardWouldMoveToArea(
            AbilityType.NonKeyword,
            None,
            lambda effect, message:
                message.SetCannot(effect),
            conditions=[
                lambda effect, message:
                    Attachment.IsType(message.trigger) and \
                    message.into_area.flags.is_discards and \
                    isinstance(message.trigger.bind_face, Identity|Ally), # Fix "06003" "21158"
            ]
        )
    ]

