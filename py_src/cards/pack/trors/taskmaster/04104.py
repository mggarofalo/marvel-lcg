from . import *

# Photographic Reflexes

def GetAbilities() -> Sequence['Ability']:

    def photographic_reflexes(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        identity = message.attacker.GetControlByPlayer().GetIdentity()

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage'):
            damage = message.PreventDamage("All", effect)
            this.DealDamage([identity], damage, effect)
            Faces.DiscardAll([this], effect)

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldTakeDamage(
                AbilityType.Temp0,
                message.targets,
                action
            ),
            unregister_after_exec=True,
        )

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Taskmaster")
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "Player",
            CardFinder(name="Taskmaster"),
            photographic_reflexes,
        ).LimitOncePerEvent()
        # AbilityFactory.WhenAttachedCardBeAttacked(
        #     AbilityType.ForcedInterrupt,
        #     lambda effect, message:
        #         message.attacker.GetController().IsPlayer(),
        #     photographic_reflexes
        # ),
    ]

