from . import *

# Need for Speed

def GetAbilities() -> Sequence['Ability']:

    def need_for_speed(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            identity = player.GetIdentity()
            Faces.ExhaustAll([identity], effect)
            identity.CannotReadyDuringNextTurn(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Pietro Maximoff → remove Need for Speed from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Exhaust your identity. You cannot ready your identity until your next turn ends. Discard this obligation",
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            need_for_speed
        )
    ]

