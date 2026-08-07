from . import *

# Gene Pool

def GetAbilities() -> Sequence['Ability']:

    def gene_pool(effect: 'Effect', message: 'Message.AfterUnitBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], 3, effect)


    return [
        AbilityFactory.AfterUnitBeDefeated(
            AbilityType.ForcedResponse,
            Ally,
            gene_pool,
            by_consequential=False,
        ),
    ]

