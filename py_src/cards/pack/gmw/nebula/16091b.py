from . import *

# The Art of Evasion

def GetAbilities() -> Sequence['Ability']:

    def the_art_of_evasion(effect: 'Effect') -> int:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        ship = FindNebulaShip(effect)
        if ship:
            return Worlds.GetPlayerNumIcon(effect) * ship.GetCounters('evasion')
        return 0


    return [
        AbilityFactory.WhenCalcThisSchemeEscalation(
            the_art_of_evasion
        ),
    ]

