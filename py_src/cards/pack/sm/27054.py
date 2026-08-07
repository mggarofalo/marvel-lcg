from . import *

# Government Liaison

def GetAbilities() -> Sequence['Ability']:

    def government_liaison(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.PlayCardsLikeInTurn(effect.targets, effect, update_resources_cost=-1)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            government_liaison
        ).SetTarget(trait="S.H.I.E.L.D", from_where=["YourHandCards"])
        .SetCostFunc(CostFunc.Exhaust("This")),
    ]

