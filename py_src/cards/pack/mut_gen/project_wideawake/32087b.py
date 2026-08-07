from . import *

# Night of the Sentinels B

def GetAbilities() -> Sequence['Ability']:

    def night_of_the_sentinels_a(effect: 'Effect', message: 'Message.AfterSchemePlaceThreat') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        player = Worlds.GetFirstPlayer(effect)
        PlaceDeckTopCardFacedownUnderOperationZeroTolerance(player, effect)
        this.RemoveThreatFromSchemes([this], "5*", effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Operation Zero Tolerance"),
            permanent=1,
        ),
        AbilityFactory.AfterSchemePlaceThreat(
            AbilityType.ForcedResponse,
            "This",
            night_of_the_sentinels_a,
            has_at_least_threat="5*"
        ),
    ]

