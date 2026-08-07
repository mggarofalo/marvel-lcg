from . import *

# * Deadpool Corps Ship

def GetAbilities() -> Sequence['Ability']:

    def deadpool_corps_ship(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        ally = effect.targets[0].CastTo(Ally)
        ally.PutIntoPlay(initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            deadpool_corps_ship
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealPlayerEncounterCard(
            1, "Initiator"))
        .SetTarget(Ally, card_class="'Pool", from_where=["YourHandCards"]),
    ]

