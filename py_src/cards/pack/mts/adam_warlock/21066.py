from . import *

# Regeneration Cycle

def GetAbilities() -> Sequence['Ability']:

    def regeneration_cycle(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            faces = player.DiscardDeckTopCards(5, effect)
            value = FacesCounter.GetAspectCount(faces)
            this.PlaceThreatOnSchemes("MainScheme", value, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust your alter-ego → remove this card from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
            ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
            AbilityFactory.ForChoiceAbility(
                "Discard the top 5 cards of your deck",
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            regeneration_cycle
        )
    ]

