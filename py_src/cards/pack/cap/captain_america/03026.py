from . import *

# Man Out of Time

def GetAbilities() -> Sequence['Ability']:

    def man_out_of_time(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        def discard_hand_of_handcards_and_discard_this(targets: Sequence['CardFace']) -> None:
            cards_size = player.hand_cards.GetSize()//2
            assert cards_size == len(targets), f"{cards_size=} {len(targets)=}"
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        YouMayFlipToYourAlterEgoForm(player, effect)

        half_hand_size = int(player.hand_cards.GetSize() * .5)
        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Steve Rogers → remove Man Out of Time from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard half of the cards in your hand, rounded down. Discard this obligation",
                discard_hand_of_handcards_and_discard_this
            ).SetTarget(range=(half_hand_size, half_hand_size), from_where=["YourHandCards"], canbe_discard=True)
        )

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            man_out_of_time
        )
    ]
