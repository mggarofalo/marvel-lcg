from . import *

# Warp Drive Initiated

def GetAbilities() -> Sequence['Ability']:

    def warp_drive_initiated_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        ship = FindNebulaShip(effect)
        if ship:
            Faces.PlaceCountersOn([ship], 2, 'evasion', effect)
            num = ship.GetCounters('evasion') * 2
            def action(player: 'Player'):
                player.DiscardDeckTopCards(num, effect)

            Players.ForEachPlayer(effect, action)
            Worlds.DiscardEncounterCards(num, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            warp_drive_initiated_revealed
        ),
    ]

