from . import *

# Drone Factory

def GetAbilities() -> Sequence['Ability']:

    def drone_factory(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        def action(player: 'Player'):
            CreateUltronFacedownDrone(player, 1, effect)

        Players.ForEachPlayer(effect, action)

        count = 0
        for unit in Worlds.GetOnFieldMinions(effect):
            if unit.HasTrait("DRONE"):
                count += 1

        this.PlaceThreatOnSchemes([this], count, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            drone_factory
        )
    ]
