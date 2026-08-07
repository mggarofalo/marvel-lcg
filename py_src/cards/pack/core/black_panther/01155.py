from . import *

# Affairs of State

def GetAbilities() -> Sequence['Ability']:

    def affairs_of_state(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def discard_black_panther_upgrade_and_discard_this(targets: Sequence['CardFace']) -> None:
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust T'Challa → remove Affairs of State from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Choose and discard a BLACK PANTHER upgrade you control. Discard this obligation",
                discard_black_panther_upgrade_and_discard_this
            ).SetTarget(Upgrade, trait="BLACK PANTHER", from_where=["YouControlCards"], canbe_discard=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            affairs_of_state
        )
    ]

