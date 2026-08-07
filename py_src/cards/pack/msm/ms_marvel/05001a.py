from . import *

# * Ms. Marvel

def GetAbilities() -> Sequence['Ability']:

    def ms_marvel(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder(traits=["ATTACK", "THWART", "DEFENSE"], card_type=Event),
            ms_marvel,
        ).SetName("Morphogenetics")
        .SetTarget("PlayedCard")
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

