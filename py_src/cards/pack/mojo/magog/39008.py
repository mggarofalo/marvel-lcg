from . import *

# Pump Up the Crowd

def GetAbilities() -> Sequence['Ability']:

    def pump_up_the_crowd_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        champion = FindTheChampion(effect)
        if champion:
            if champion.HasTrait("CHEERING CROWD"):
                this.PlaceThreatOnSchemes([this], "1*", effect)

    def pump_up_the_crowd(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        challengers = FindTheChallengers(effect)
        if challengers:
            Faces.PlaceCountersOn([challengers], "1*", 'ratings', effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            pump_up_the_crowd_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            pump_up_the_crowd
        )
    ]

