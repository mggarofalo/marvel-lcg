from . import *

# Surge of Aggression

def GetAbilities() -> Sequence['Ability']:

    def surge_of_aggression(effect: 'Effect', message: 'Message.AfterUnitHitPointReset') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        challengers = FindTheChallengers(effect)
        if challengers:
            Faces.PlaceCountersOn([challengers], "1*", 'ratings', effect)
            Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="MaGog")
        ),
        AbilityFactory.AfterUnitHitPointReset(
            AbilityType.ForcedResponse,
            CardFinder(name="MaGog"),
            surge_of_aggression
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

