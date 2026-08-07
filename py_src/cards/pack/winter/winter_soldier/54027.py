from . import *

# Red Room Programming

def GetAbilities() -> Sequence['Ability']:

    def red_room_programming(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            value = FacesCounter.GetTotalCost(targets)
            player.GetIdentity().TakeIndirectDamage(this, value, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Bucky Barnes → remove this card from the game.",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard the highest printed cost card from your hand and take indirect damage equal to its cost. Discard this obligation.",
                action
            ).SetTarget("YourHandCards", highest_cost=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            red_room_programming
        ),
    ]

