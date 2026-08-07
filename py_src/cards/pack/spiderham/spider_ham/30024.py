from . import *

# "I Really Want a Hot Dog!"

def GetAbilities() -> Sequence['Ability']:

    def i_really_want_a_hot_dog(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            if player.GetIdentity().IsStunned():
                ThisCardGainSurge(effect)
            else:
                Faces.GiveStatus([player.GetIdentity()], "Stunned", effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Peter Porker and remove 1 toon counter from him",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
            .SetCostFunc(CostFunc.Counter("YourIdentity", 1, 'toon')),
            AbilityFactory.ForChoiceAbility(
                "You are stunned. If you are already stunned, this cards gains surge",
                action
            ).SetTarget("YourIdentity")
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            i_really_want_a_hot_dog
        )
    ]

