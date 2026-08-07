from . import *

# The Odd Couple

def GetAbilities() -> Sequence['Ability']:

    def the_odd_couple(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        initiator = effect.GetInitiator()

        Faces.ShuffleAllTo(effect.targets, initiator.player_deck,  effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            ally_limit=-2,
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
            ex_operation=the_odd_couple
        ).SetCostFunc(CostFunc.Exhaust("YouControlUnit", range=(2, 2)))
        .SetTarget(Ally, from_where=["YourDiscardPile"])
    ]

