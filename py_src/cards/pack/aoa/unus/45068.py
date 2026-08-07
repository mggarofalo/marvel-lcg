from . import *

# Endless Ranks

def GetAbilities() -> Sequence['Ability']:

    def endless_ranks(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        scheme = GetGenePool(effect)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 3, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            endless_ranks
        ),
    ]

