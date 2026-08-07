from . import *

# Red Dreams

def GetAbilities() -> Sequence['Ability']:

    def red_dreams(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            # player.DiscardAllHands(effect, CardFinder(has_printed_res="B"))
            Faces.DiscardAll(targets, effect)
            player.GetIdentity().TakeDamage(this, 1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Nadia Van Dyne → remove Red Dreams from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard each card with a printed [mental] resource from your hand and take 1 damage. Discard this obligation",
                action
            ).SetTarget(has_printed_res="B", range="All", from_where=["YourHandCards"], canbe_discard=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            red_dreams
        )
    ]

