from . import *

# Mind Scan

def GetAbilities() -> Sequence['Ability']:

    def mind_scan_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = 2 + Worlds.VictoryDisplay(effect).FindCardSize(CardFinder(card_type=SchemeSide2))
        this.PlaceThreatOnSchemes("MainScheme", value, effect)

    def mind_scan_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mind_scan_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            mind_scan_boost
        ),
    ]

