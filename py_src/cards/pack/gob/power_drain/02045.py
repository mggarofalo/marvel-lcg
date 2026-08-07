from . import *

# Shock Therapy

def GetAbilities() -> Sequence['Ability']:

    def shock_therapy_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        faces = Worlds.DiscardEncounterCards("1*", effect)
        size = FacesCounter.CountTotalBoostIcons(faces)

        villain = Worlds.FindVillain(effect)
        if villain:
            this.HealthUnits([villain], size, effect)


    def shock_therapy_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        Worlds.DiscardEncounterCards(3, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            shock_therapy_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            shock_therapy_boost
        ),
    ]

