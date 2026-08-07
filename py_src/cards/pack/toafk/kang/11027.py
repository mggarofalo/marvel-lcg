from . import *

# Manipulated Timestream

def GetAbilities() -> Sequence['Ability']:

    def manipulated_timestream(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if not player.DiscardHandCards("All", effect, card_type=Event):
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            manipulated_timestream
        )
    ]

