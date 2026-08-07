from . import *

# Spectacular!

def GetAbilities() -> Sequence['Ability']:

    def spectacular_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        # player = message.GetToPlayer()
        # identity = player.GetIdentity()

        this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        if Worlds.IsCompetitiveMode(effect):
            ...


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            spectacular_revealed
        ),
    ]

