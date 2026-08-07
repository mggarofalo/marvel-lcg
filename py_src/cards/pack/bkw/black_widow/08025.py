from . import *

# Burn Notice

def GetAbilities() -> Sequence['Ability']:

    def burn_notice(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            if targets:
                Faces.DiscardAll(targets, effect)
            else:
                ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Natasha Romanoff → remove this card from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard the [[Preparation]] card you control with the highest cost",
                action
            ).SetTarget(trait="PREPARATION", canbe_discard=True, highest_cost=True, from_where=["YouControlCards"]),
            AbilityFactory.Otherwise(
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            burn_notice
        )
    ]

