from . import *

# Stolen Memories

def GetAbilities() -> Sequence['Ability']:

    def stolen_memories(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        player = message.GetToPlayer()

        this.PlaceCardHere(player.player_deck.GetTops(8), False, effect)

        # player.PopPlayerDeckCards(
        #     8,
        #     lambda face: this.PlaceCardHere(face, False, effect, ui_group=True),
        #     effect,
        #     is_group_action="Set"
        # )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            stolen_memories
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            has_printed_res="B"
        ))
    ]

