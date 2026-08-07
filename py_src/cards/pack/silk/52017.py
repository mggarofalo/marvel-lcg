from . import *

# * Dr. Sinclair

def GetAbilities() -> Sequence['Ability']:

    def dr_sinclair(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetAlterEgo()
        value = identity.recover
        this.HealthUnits(effect.targets, value, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard 1 status card from your identity",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(StatusCard, bind_to=identity, canbe_discard=True),
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            dr_sinclair
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("B"))
        .SetTarget("YourAlterEgo", canbe_heal=True)
        .AnyPlayerCanDoThis(),
    ]

