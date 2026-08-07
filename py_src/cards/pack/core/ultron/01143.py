from . import *

# Advanced Ultron Drone

def GetAbilities() -> Sequence['Ability']:

    def advanced_ultron_drone(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        player = this.GetEngagedPlayer()
        CreateUltronFacedownDrone(player, 1, effect)

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            advanced_ultron_drone
        )
    ]

