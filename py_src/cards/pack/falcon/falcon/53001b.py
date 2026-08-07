from . import *

# * Sam Wilson

def GetAbilities() -> Sequence['Ability']:

    def sam_wilson(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.SearchForCard(
            effect,
            initiator,
            include_player_deck=True,
            include_player_discard_pile=True,
            trait="BIRD",
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            sam_wilson
        ).SetName("Birds of a Feather")
        .SetCostFunc(CostFunc.Discard("YourHandCards"))
        .LimitOncePerRound(),
    ]

