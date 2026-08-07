from . import *

# Undermine Support

def GetAbilities() -> Sequence['Ability']:

    def undermine_support(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        RemoveSecretCountersFromAmongBoardMemberEnvironments("1*", effect)


    return [
        AbilityFactory.AdditionalCostToReady(
            Support,
            CostFunc.Spend(Cost("1")),
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            undermine_support,
        ),
    ]

