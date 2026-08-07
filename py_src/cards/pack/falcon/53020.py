from . import *

# Flight Squadron

def GetAbilities() -> Sequence['Ability']:

    def flight_squadron(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="AERIAL"),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "You",
            if_each_of_your_allies_has_trait="AERIAL",
            ally_limit=1,
        ),
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("AERIAL"),
            flight_squadron,
            conditions=[
                lambda effect, message:
                    Faces.EachOfAre(effect.GetInitiator().GetControlAllies(), CardFinder2("AERIAL"))
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YouControlUnit", card_type=Ally, canbe_ready=True),
    ]

