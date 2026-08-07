from . import *

# Business Problems

def GetAbilities() -> Sequence['Ability']:

    def business_problems(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        def exhaust_each_upgrade_you_control_discard_this_obligation(targets: Sequence['CardFace']) -> None:
            Faces.ExhaustAll(targets, effect)
            Faces.DiscardAll([this], effect)

        YouMayFlipToYourAlterEgoForm(player, effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Tony Stark → remove Business Problems from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Exhaust each upgrade you control. Discard this obligation",
                exhaust_each_upgrade_you_control_discard_this_obligation
            ).SetTarget(Upgrade, canbe_exhaust=True,
                range="All", from_where=["YouControlCards"]),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )

    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            business_problems
        )
    ]
