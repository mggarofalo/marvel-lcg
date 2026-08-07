from . import *

# Order and Chaos

def GetAbilities() -> Sequence['Ability']:

    def order_and_chaos(effect: 'Effect', message: 'Message.WhenPlayerRevealCard') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.CancelWhenRevealedEffect(effect)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    this.DealDamage(targets, 2, effect)
            ).SetTarget(Villain)
        )


    return [
        AbilityFactory.WhenPlayerRevealCard(
            AbilityType.HeroInterrupt,
            "AnyPlayer",
            Treachery,
            "WhenRevealed",
            order_and_chaos,
            from_encounter=True,
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp")
    ]

