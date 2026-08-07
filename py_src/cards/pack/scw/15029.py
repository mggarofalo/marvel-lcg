from . import *

# Last Stand

def GetAbilities() -> Sequence['Ability']:

    def last_stand(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(+3, effect)
        message.trigger.effect.RegisterTemp(
            AbilityFactory.AfterUnitAttackUnit(
                AbilityType.Temp0,
                Ally,
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
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.HeroInterrupt,
            Ally,
            last_stand,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator() == message.trigger.GetControlBy()
            ],
        ).SetPlay().SetLabel()
        .SetTarget("Trigger"),
    ]

