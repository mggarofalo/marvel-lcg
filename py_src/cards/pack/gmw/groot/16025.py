from . import *

# Wilt

def GetAbilities() -> Sequence['Ability']:

    def wilt(effect: 'Effect', message: 'Message.WhenObligationGiveToPlayer') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)
        player = message.GetGaveToPlayer()

        YouMayFlipToYourAlterEgoForm(player, effect)

        def action(targets: Sequence['CardFace']):
            if Faces.RemoveCountersOn(targets, 3, 'growth', effect) == 0:
                ThisCardGainSurge(effect)
            Faces.DiscardAll([this], effect)

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Exhaust your alter-ego → remove Wilt from the game",
                lambda targets:
                    Faces.RemoveAllFromGame([this], effect),
            ).SetCostFunc(CostFunc.Exhaust("YourAlterEgo")),
            AbilityFactory.ForChoiceAbility(
                "Remove 3 growth counters from Groot. If no growth counters were removed this way, this card gains surge. Discard this obligation",
                action
            ).SetTarget(name="Groot"),
        )


    return [
        AbilityFactory.WhenThisObligationGiveToYou(
            wilt
        )
    ]

