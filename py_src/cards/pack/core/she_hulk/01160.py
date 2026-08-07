from . import *

# Legal Work

def GetAbilities() -> Sequence['Ability']:

    def legal_work(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            scheme = Worlds.FindMainScheme(effect)
            if scheme:
                scheme.PlaceAccelerationToken(1, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Jennifer Walters → remove Legal Work from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Give the main scheme 1 acceleration token. Discard this obligation",
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            legal_work
        )
    ]

