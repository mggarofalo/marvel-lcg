from . import *

# Spider-Tingle

def GetAbilities() -> Sequence['Ability']:

    def spider_tingle(effect: 'Effect', message: 'Message.WhenCardWouldReveal') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        def cancel_when_reaveal_and_discard_this(effect: 'Effect', message: 'Message.WhenPlayerRevealCard'):
            message.CancelWhenRevealedEffect(effect)
            Faces.DiscardAll([this], effect)

        this.effect.RegisterTemp(
            AbilityFactory.WhenPlayerRevealCard(
                AbilityType.Temp0,
                "AnyPlayer",
                Treachery,
                "WhenRevealed",
                cancel_when_reaveal_and_discard_this,
                conditions=[
                    lambda reveal_effect, reveal_message:
                        message.trigger == reveal_message.trigger,
                ]
            ),
            unregister_after_exec=True,
            until_event_end=message
        )


    return [
        AbilityFactory.WhenCardWouldReveal(
            AbilityType.Interrupt,
            "You",
            None,
            spider_tingle
        ).SetCostFunc(CostFunc.DealDamage(1,
            "YouControlUnit",
            trait="WEB-WARRIOR"))
        .SetTarget("Trigger"),
    ]

