from . import *

# Future Weapon

def GetAbilities() -> Sequence['Ability']:

    def future_weapon(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)

        def stunned(damage_effect: 'Effect', damage_message: 'Message.AfterUnitTookDamage'):
            Faces.GiveStatus(damage_effect.targets, "Stunned", effect)

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitTookDamage(
                AbilityType.Temp0,
                Hero,
                stunned,
                conditions=[
                    lambda damage_effect, damage_message:
                        message == damage_message.would_atk_message,
                ]
            ).SetTarget("Trigger"),
            unregister_after_exec=True,
        )

        this.effect.RegisterTemp(
            AbilityFactory.AfterUnitAttackUnit(
                AbilityType.Temp0,
                message.attacker,
                None,
                DiscardThisCard,
                conditions=[
                    lambda atk_effect, atk_message:
                        message == atk_message.would_atk_message,
                ]
            ),
            unregister_after_exec=True,
        )


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Kang")
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Kang"),
            future_weapon
        )
    ]

