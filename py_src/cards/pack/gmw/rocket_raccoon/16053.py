from . import *

# Crisis on Halfworld

def GetAbilities() -> Sequence['Ability']:

    def crisis_on_halfworld(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            face = player.DiscardControlCards(effect, upgrade=True, highest_cost=True)
            if not face:
                ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust your alter-ego → remove Crisis on Halfworld from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
            AbilityFactory.ForChoiceAbility(
                "Discard the highest cost upgrade you control. If no upgrade was discarded this way, this card gains surge. Discard this obligation",
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            crisis_on_halfworld
        )
    ]

