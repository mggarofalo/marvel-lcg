from . import *

# * Mockingbird: Bobbi Morse

def GetAbilities() -> Sequence['Ability']:

    def give_prevent_damage_effect(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)

        def prevent_damage(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
            message.PreventDamage("All", effect)

        would_atk_message = message
        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldTakeDamage(
                AbilityType.Temp0,
                None,
                prevent_damage,
                conditions=[
                    lambda effect, message:
                        message.would_atk_message == would_atk_message,
                ]
            ),
            unregister_after_exec=True,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenUnitInitiatesAttackAgainst(
            AbilityType.Interrupt,
            Villain,
            "You",
            give_prevent_damage_effect,
        ).SetCost(Cost("1"))
        .SetCostFunc(CostFunc.ReturnToHand("This", to_who="Initiator"))
    ]

