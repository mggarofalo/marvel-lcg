from . import *

# Homesick

def GetAbilities() -> Sequence['Ability']:

    def homesick(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll([this], effect)
            if player.GetIdentity().DiscardAllTough(effect) == 0:
                ThisCardGainSurge(effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Piotr Rasputin → remove Homesick from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard this card and each tough status card from your identity",
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            homesick
        )
    ]

