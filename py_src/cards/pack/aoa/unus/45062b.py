from . import *

# Hunting Gene Traitors

def GetAbilities() -> Sequence['Ability']:

    def hunting_gene_traitors(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        scheme = GetGenePool(effect)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 1, effect)

    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            hunting_gene_traitors
        ),
    ]

