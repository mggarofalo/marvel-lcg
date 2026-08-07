from . import *

# * Mister Fear

def GetAbilities() -> Sequence['Ability']:

    def mister_fear_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardDeckUntil(
            effect,
            card_type=Ally
        )


    return [
        AbilityFactory.AdditionalCostToReady(
            CardFinder(card_type=Hero|Ally),
            CostFunc.Spend(Cost("B")),
            control_by="EngagedPlayer"
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            mister_fear_boost
        ),
    ]

