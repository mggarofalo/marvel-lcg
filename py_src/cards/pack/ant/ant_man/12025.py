from . import *

# Care for Cassie

def GetAbilities() -> Sequence['Ability']:

    def care_for_cassie(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def choose_and_discard_1_card_from_your_hand_you_cannot_change_form_until_your_next_turn_ends_discard_this_obligation(targets: Sequence['CardFace']) -> None:
            Faces.DiscardAll(targets, effect)
            Players.CannotChangeFormDuringNextTurn(player, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Scott Lang → remove Care for Cassie from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Choose and discard 1 card from your hand. You cannot change form until your next turn ends. Discard this obligation",
                choose_and_discard_1_card_from_your_hand_you_cannot_change_form_until_your_next_turn_ends_discard_this_obligation
            ).SetTarget(from_where=["YourHandCards"], canbe_discard=True)
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            care_for_cassie
        ),
    ]

