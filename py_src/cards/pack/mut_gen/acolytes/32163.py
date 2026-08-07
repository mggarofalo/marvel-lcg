from . import *

# * Unuscione

def GetAbilities() -> Sequence['Ability']:

    def unuscione(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            if villain.tough > 0:
                this.HealthUnits([villain], 4, effect)
            else:
                Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            unuscione
        ),
    ]

