from . import *

# Odin's Anger

def GetAbilities() -> Sequence['Ability']:

    def odins_anger(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            identity = player.GetIdentity()
            Faces.GiveStatus([identity], "Stunned", effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Odinson → remove Odin's Anger from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
            ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
            AbilityFactory.ForChoiceAbility(
                "Discard Mjolnir from your hand or from play. You are stunned. Discard this obligation",
                action
            ).SetTarget(Upgrade, name="Mjolnir",
                from_where=["YourHandCards", "YouControlCards"], canbe_discard=True),
            AbilityFactory.Otherwise(
                action
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            odins_anger
        )
    ]

