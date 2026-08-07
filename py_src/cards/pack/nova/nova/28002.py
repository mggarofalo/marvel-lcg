from . import *

# * Ms. Marvel: Kamala Khan

def GetAbilities() -> Sequence['Ability']:

    def ms_marvel(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            Event,
            ms_marvel,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealDamage(1, "This"))
        .SetTarget("PlayedCard", from_where=["YourDiscardPile"]),
    ]

