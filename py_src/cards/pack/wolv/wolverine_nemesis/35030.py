from . import *

# Death Factor

def GetAbilities() -> Sequence['Ability']:

    def death_factor(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    def discard_this_instead(effect: 'Effect', message: 'Message.WhenUnitWouldRecovery') -> None:
        this = effect.this.CastTo(Attachment)
        message.SetBeInstead(effect)
        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.ForcedResponse,
            "AttachedPlayer",
            death_factor
        ).SetTarget("Attached"),
        AbilityFactory.WhenUnitMakeRecovery(
            AbilityType.AlterEgoInterrupt,
            "AttachedIdentity",
            discard_this_instead,
            is_basic_recovery=True
        )
    ]

