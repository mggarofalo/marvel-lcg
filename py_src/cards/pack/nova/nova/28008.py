from . import *

# * Jesse Alexander

def GetAbilities() -> Sequence['Ability']:

    def jesse_alexander(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)


        initiator = effect.GetInitiator()
        Faces.ShuffleAllTo(effect.targets, initiator.player_deck, effect)
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            jesse_alexander
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="Connection to the Worldmind", from_where=["YourDiscardPile"])
        .SetHasNoTargetEffect(),
    ]

