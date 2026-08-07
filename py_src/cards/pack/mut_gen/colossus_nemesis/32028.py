from . import *

# Unstoppable

def GetAbilities() -> Sequence['Ability']:

    def unstoppable(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)
        message.GainPiercing(effect)

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitAttackEnd(
                AbilityType.Temp0,
                message.attacker,
                DiscardThisCard,
                conditions=[
                    lambda effect, end_atk_message:
                        message in end_atk_message.would_atk_messages,
                ]
            ),
            unregister_after_exec=True,
        )


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            highest_printed_atk=True,
            without_another_copy=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            unstoppable
        ),
    ]

