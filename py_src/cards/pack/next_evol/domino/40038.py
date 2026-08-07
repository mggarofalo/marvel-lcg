from . import *

# * Diamondback: Rachel Leighton

def GetAbilities() -> Sequence['Ability']:

    def diamondback(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        faces = effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards
        value = FacesCounter.GetPrintedResourcesIcon(faces, initiator.player_deck)

        this.DealDamage(effect.targets, value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            diamondback
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealDamage(1, "This"))
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", 1))
        .SetTarget(Enemy, range="All"),
    ]

