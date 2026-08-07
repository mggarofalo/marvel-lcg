from . import *

# Webbed Up

def GetAbilities() -> Sequence['Ability']:

    def webbed_up(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenPlayerWouldPlayCard') -> None:
        this = effect.this.CastTo(Attachment)
        Faces.DiscardAll([this], effect)
        message.SetBeInstead(effect)
        Faces.GiveStatus([message.trigger.GetControlByPlayer().GetIdentity()], "Stunned", effect)

    def webbed_up_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            webbed_up_boost
        ),
        *AbilityFactory.WhenAttachedPlayerWouldAttack(
            AbilityType.ForcedInterrupt,
            "YourHero",
            webbed_up
        )
    ]

