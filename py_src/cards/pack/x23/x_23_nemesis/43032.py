from . import *

# Critical Wound

def GetAbilities() -> Sequence['Ability']:

    def critical_wound(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.DiscardAll([this], effect)
        identity.TakeDamage(this, 4, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity"
        ),
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.ForcedInterrupt,
            "You",
            critical_wound
        ),
    ]

