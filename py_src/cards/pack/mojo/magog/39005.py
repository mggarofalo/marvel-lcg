from . import *

# Jolt of Adrenaline

def GetAbilities() -> Sequence['Ability']:

    def jolt_of_adrenaline(effect: 'Effect', message: 'Message.AfterUnitHitPointReset') -> None:
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
            jolt_of_adrenaline
        ),
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="MaGog"),
            retaliate=1,
            stalwart=1,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            RevealThisCard
        ),
    ]

