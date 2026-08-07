from . import *

# Frozen in Time

def GetAbilities() -> Sequence['Ability']:

    def frozen_in_time(effect: 'Effect', message: 'Message.WhenCardWouldReady') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.WhenCardWouldReady(
            AbilityType.ForcedInterrupt,
            "AttachedCharacter",
            frozen_in_time
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            "YouIdentity"
        )
    ]

