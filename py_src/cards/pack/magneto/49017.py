from . import *

# Squared Off

def GetAbilities() -> Sequence['Ability']:

    def squared_off(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.PlayCardsLikeInTurn(
            effect.targets,
            effect,
            update_resources_cost=-3
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            squared_off
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.DiscardDeckTopUntil("EncounterDeck", Minion, "PutIntoPlayEngagedYou"))
        .SetTarget(Ally, from_where=["YourHandCards"]),
    ]

