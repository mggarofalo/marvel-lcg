from . import *

# Fear the Future

def GetAbilities() -> Sequence['Ability']:

    def fear_the_future(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll([this], effect)
            faces = player.hand_cards.Get()
            faces = Filter.ByType(faces, Resource)
            if not faces:
                ThisCardGainSurge(effect)
            else:
                Faces.DiscardAll(faces, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Lucas Bishop → remove Fear the Future from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard this card and each resource card from your hand",
                action
            ),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            fear_the_future
        )
    ]

