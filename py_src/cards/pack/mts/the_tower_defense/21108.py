from . import *

# Bound by Blood

def GetAbilities() -> Sequence['Ability']:

    def bound_by_blood_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villains = Worlds.GetVillains(effect)
        this.HealthUnits(villains, 2, effect)
        Faces.GiveStatus(villains, "Tough", effect)


    def bound_by_blood_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        villain = Worlds.FindVillain(effect)
        if villain:
            this.HealthUnits([villain], 2, effect)
            Faces.GiveStatus([villain], "Tough", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            bound_by_blood_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            bound_by_blood_boost
        ),
    ]

