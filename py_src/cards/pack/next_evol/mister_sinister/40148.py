from . import *

# Molecular Control

def GetAbilities() -> Sequence['Ability']:

    def molecular_control_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = FindMisterSinister(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)
            if villain.HasTrait("BRUTE"):
                villain.HealthUnits([villain], 4, effect)

    def molecular_control_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = FindMisterSinister(effect)
        if villain:
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            molecular_control_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            molecular_control_boost
        ),
    ]

