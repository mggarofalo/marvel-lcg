from . import *

# Hapless Pedestrians

def GetAbilities() -> Sequence['Ability']:

    def hapless_pedestrians(effect: 'Effect', message: 'Message.AfterCardPlacedToken') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        this.DealIndirectDamage(first_player.GetIdentity(), 3, effect)


    return [
        AbilityFactory.AfterCardPlacedToken(
            AbilityType.ForcedResponse,
            "This",
            'acceleration_token',
            hapless_pedestrians,
        ),
    ]

