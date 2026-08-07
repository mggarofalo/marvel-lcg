from . import *

# * Cybernetic Arm

def GetAbilities() -> Sequence['Ability']:

    def cybernetic_arm(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldAttack(
                AbilityType.Temp0,
                "You",
                lambda effect, atk_message:
                    atk_message.DealAdditionalDamage(1, effect),
                by_effect=message.for_effect,
            ),
            unregister_after_exec=False,
            until_resolve_effect=message.for_effect
        )


    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("G"),
            for_card=CardFinder2("ATTACK", Event),
            ex_operation=cybernetic_arm
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

