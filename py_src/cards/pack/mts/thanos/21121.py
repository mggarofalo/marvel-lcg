from . import *

# Deviant Syndrome

def GetAbilities() -> Sequence['Ability']:

    def deviant_syndrome_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        placed = 0
        if villain:
            placed = Faces.GiveStatus([villain], "Tough", effect)
        if not placed:
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)

    def deviant_syndrome_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            deviant_syndrome_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            deviant_syndrome_boost
        ),
    ]

