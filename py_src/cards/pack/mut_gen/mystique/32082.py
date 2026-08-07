from . import *

# Infiltration

def GetAbilities() -> Sequence['Ability']:

    def infiltration_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        Faces.ShuffleAllTo([this], player.player_deck, effect)
        ThisCardGainSurge(effect)

    def infiltration(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, ally=True, support=True)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            infiltration_revealed
        ),
        AbilityFactory.AfterCardEnterHand(
            AbilityType.ForcedResponse,
            "This",
            infiltration
        ),
    ]

