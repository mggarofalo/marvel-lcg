from . import *

# * Wong

def GetAbilities() -> Sequence['Ability']:

    def wong(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 1 damage from your identity",
                lambda targets:
                    this.HealthUnits(targets, 1, effect)
            ).SetTarget("YourIdentity", canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Discard the top card of the [[Invocation]] deck",
                lambda targets:
                    Faces.DiscardAll(targets, effect)
            ).SetTarget(SelectInvocationTopCard, canbe_discard=True)
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            wong,
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

