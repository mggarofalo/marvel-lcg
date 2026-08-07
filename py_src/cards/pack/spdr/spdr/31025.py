from . import *

# Inherited Burden

def GetAbilities() -> Sequence['Ability']:

    def inherited_burden(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
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
                "Exhaust Peni Parker → remove Inherited Burden from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Choose and discard 1 [[Interface]] upgrade you control. Discard this obligation",
                action
            ).SetTarget(Upgrade, trait="INTERFACE", from_where=["YouControlCards"], canbe_discard=True),
            AbilityFactory.Otherwise(
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            inherited_burden
        ),
    ]

