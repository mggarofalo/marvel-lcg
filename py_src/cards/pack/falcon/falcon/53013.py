from . import *

# Vibranium Microweave

def GetAbilities() -> Sequence['Ability']:

    def vibranium_microweave(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        message.PreventDamage(1, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Deal 1 damage to an enemy",
                lambda targets:
                    this.DealDamage(targets, 1, effect)
            ).SetTarget(Enemy)
        )


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Falcon"),
            defense=1
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            CardFinder(name="Falcon"),
            vibranium_microweave
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

