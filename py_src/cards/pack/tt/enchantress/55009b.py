from . import *

# Trance of Pride

def GetAbilities() -> Sequence['Ability']:

    def trance_of_pride_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if not Faces.ExhaustAll([identity], effect):
            player.DiscardRandomHandCards(1, effect)

    def trance_of_pride(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.PlaceThreatOnSchemes(effect.targets, 1, effect)
        this.RemoveThreatFromSchemes(effect.targets2, 2, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="ENTHRALLED"
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            trance_of_pride_revealed
        ),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.ForcedAction,
            trance_of_pride
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(MainScheme, can_place_threat=True)
        .SetTarget2(SchemeSide2)
    ]

