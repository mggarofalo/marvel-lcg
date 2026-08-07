from . import *

# * Batroc

def GetAbilities() -> Sequence['Ability']:

    def batroc(effect: 'Effect', message: 'Message.WhenMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        player.DiscardHandCards((1, 1), effect)

    return [
        AbilityFactory.WhenMinionEngagePlayer(
            AbilityType.ForcedInterrupt,
            "This",
            "You",
            batroc
        ),
    ]

