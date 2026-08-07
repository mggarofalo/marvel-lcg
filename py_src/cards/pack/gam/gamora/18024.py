from . import *

# Unfulfilled Destiny

def GetAbilities() -> Sequence['Ability']:

    def unfulfilled_destiny(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Gamora → remove Unfulfilled Destiny from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Choose and discard 2 events from your hand. Discard this obligation",
                action
            ).SetTarget(Event, from_where=["YourHandCards"], range=(2, 2), canbe_discard=True),
            AbilityFactory.Otherwise(
                action
            ).SetTarget(Event, from_where=["YourHandCards"], range=("Zero", "All"), canbe_discard=True),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            unfulfilled_destiny
        ),
    ]

