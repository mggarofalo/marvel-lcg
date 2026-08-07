from . import *

# Safehouse

def GetAbilities() -> Sequence['Ability']:

    def safehouse(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 2 damage from your identity",
                lambda targets:
                    this.HealthUnits(targets, 2, effect)
            ).SetTarget("YourIdentity", canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Draw 1 card",
                lambda targets:
                    initiator.DrawUp(1, effect)
            )
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            safehouse
        ).AnyPlayerCanDoThis()
        .LimitOncePerRoundPerPlayer(),
    ]

