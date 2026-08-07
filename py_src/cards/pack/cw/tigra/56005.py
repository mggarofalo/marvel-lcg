from . import *

# Sharp Claws

def GetAbilities() -> Sequence['Ability']:

    def sharp_claws(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        message.GainATKForThisAttack(1, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "This attack gains overkill",
                lambda targets:
                    message.GainOverKill(effect),
                condition=not message.property.overkill
            ),
            AbilityFactory.ForChoiceAbility(
                "This attack gains piercing",
                lambda targets:
                    message.GainPiercing(effect),
                condition=not message.property.piercing
            )
        )


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.Interrupt,
            "You",
            sharp_claws,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourHero"),
    ]

