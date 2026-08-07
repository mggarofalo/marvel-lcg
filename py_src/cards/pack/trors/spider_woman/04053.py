from . import *

# Uncertain Loyalties

def GetAbilities() -> Sequence['Ability']:

    def uncertain_loyalties(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            this.PlaceThreatOnSchemes(targets, 3, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Jessica Drew → remove Uncertain Loyalties from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
            ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
            AbilityFactory.ForChoiceAbility(
                "Place 3 threat on the main scheme. Discard this obligation",
                action
            ).SetTarget(MainScheme, can_place_threat=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            uncertain_loyalties
        )
    ]

