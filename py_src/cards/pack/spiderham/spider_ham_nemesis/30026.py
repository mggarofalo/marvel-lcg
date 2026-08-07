from . import *

# * The Green Gobbler

def GetAbilities() -> Sequence['Ability']:

    def the_green_gobbler_boost(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.engaged_player
        for face in player.GetControlCards():
            if isinstance(face, CanPlaceCounter):
                face.RemoveAllCounters(effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            the_green_gobbler_boost
        ),
    ]

