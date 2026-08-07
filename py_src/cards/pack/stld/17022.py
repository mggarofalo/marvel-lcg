from . import *

# * Knowhere

def GetAbilities() -> Sequence['Ability']:

    def knowhere(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            # player = message.play_effect.GetInitiator()
            player.DrawUp(1, effect)


    return [
        # when Star-Lord is in hero form, each ally he plays is given
        # the Guardian trait. He is then able to use Knowhere’s response ability
        # with any ally he plays while in hero form.
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            ally_limit=1,
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "AnyPlayer",
            CardFinder2("GUARDIAN", Ally),
            knowhere,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger")
    ]

