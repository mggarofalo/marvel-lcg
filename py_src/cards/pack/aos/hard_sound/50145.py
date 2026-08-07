from . import *

# Hard Sound Bindings

def GetAbilities() -> Sequence['Ability']:

    def hard_sound_bindings(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenPlayerWouldPlayCard') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)

        player = message.trigger.GetControlByPlayer()
        identity = player.GetIdentity()

        Faces.DiscardAll([this], effect)
        Faces.GiveStatus([identity], "Stunned", effect)

    def hard_sound_bindings_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.IsStunned():
            message.GiveActivatingEnemyAdditionalBoostCard(1, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        *AbilityFactory.WhenAttachedPlayerWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedIdentity",
            hard_sound_bindings
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hard_sound_bindings_boost
        ),
    ]

