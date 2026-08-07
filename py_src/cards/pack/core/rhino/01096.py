from . import *

# * Rhino (III)

def GetAbilities() -> Sequence['Ability']:

    def rhino(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)
        
        heroes = Worlds.GetOnFieldHeroes(effect)
        Faces.GiveStatus(heroes, "Stunned", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rhino
        )
    ]
