from . import *

# Probability Field

def GetAbilities() -> Sequence['Ability']:

    def probability_field(effect: 'Effect', message: 'Message.WhenUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = FacesCounter.GetPrintedResourcesIcon(faces, initiator.player_deck)

        message.GainValue(value, effect)


    return [
        AbilityFactory.WhenUnitUseBasicPower(
            AbilityType.Interrupt,
            "You",
            probability_field
        ).SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", 1))
    ]

