from . import *

# Criminal Past

def GetAbilities() -> Sequence['Ability']:

    def criminal_past(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def discard_hawkeyes_bow_and_discard_this(targets: Sequence['CardFace']) -> None:
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Clint Barton → remove this card from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard Hawkeye's Bow from play. Discard this obligation",
                discard_hawkeyes_bow_and_discard_this,
            ).SetTarget(name=HAWKEYE_BOW_NAME, canbe_discard=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            criminal_past
        )
    ]

