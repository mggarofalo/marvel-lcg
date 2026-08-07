from . import *

# Crisis of Faith

def GetAbilities() -> Sequence['Ability']:

    def crisis_of_faith(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            Faces.DiscardAll(targets, effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust Kurt Wagner → remove Crisis of Faith from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
                condition=player.IsAlterEgo(),
            ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
            AbilityFactory.ForChoiceAbility(
                "Discard each [[Attack]] and [[Defense]] event from your hand. Discard this obligation",
                action
            ).SetTarget(Event, traits=["ATTACK", "DEFENSE"], range="All", select_rule="MustIncludeTraits", from_where=["YourHandCards"], canbe_discard=True),
            AbilityFactory.Otherwise(
                lambda targets:
                    Faces.DiscardAll([this], effect)
            )
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            crisis_of_faith
        ),
    ]

