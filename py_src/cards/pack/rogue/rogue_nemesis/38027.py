from . import *

# Misled

def GetAbilities() -> Sequence['Ability']:

    def misled_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        Faces.ShuffleAllTo([this], player.player_deck, effect)
        ThisCardGainSurge(effect)

    def misled(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            misled_revealed
        ),
        AbilityFactory.AfterCardEnterHand(
            AbilityType.ForcedResponse,
            "This",
            misled
        ),
    ]

