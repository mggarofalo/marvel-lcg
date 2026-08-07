from . import *

# * SP//dr Suit

def GetAbilities() -> Sequence['Ability']:

    def spdr_suit(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = initiator.GetControlUpgrade(CardFinder(canbe_exhaust=True, trait="INTERFACE"))
        return FacesCounter.GetPrintedResources(faces)

    def spdr_suit_res(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> 'Resources':
        faces = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        return FacesCounter.GetPrintedResources(faces)

    return [
        AbilityFactory.DoGenerateResources(
            AbilityType.Resource,
            "This",
            res_fn=spdr_suit_res,
        ).SetCostFunc(CostFunc.Exhaust(
            trait="INTERFACE",
            card_type=Upgrade,
            from_where=["YouControlCards"],
            range=(1, "All")))
        .SetName("Sync Ratio"),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            resources_fn=spdr_suit
        ),
    ]

