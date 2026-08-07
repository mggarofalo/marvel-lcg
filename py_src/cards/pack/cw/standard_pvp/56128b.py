from . import *

# Now It's Personal

def GetAbilities() -> Sequence['Ability']:

    def now_its_personal(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        # player = message.GetGaveToPlayer()

        assert False

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "",
                action
            ),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            now_its_personal
        ).SetCostFunc(CostFunc.RemoveFromGame("This")),
    ]

