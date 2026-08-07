from . import *

# Divided Loyalties

def GetAbilities() -> Sequence['Ability']:

    def divided_loyalties(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        RemoveSecretCountersFromAmongBoardMemberEnvironments("1*", effect)


    return [
        AbilityFactory.AdditionalCostToAttack(
            Ally,
            CostFunc.Spend(Cost("1")),
        ),
        AbilityFactory.AdditionalCostToThwart(
            Ally,
            CostFunc.Spend(Cost("1")),
        ),
        AbilityFactory.AdditionalCostToDefense(
            Ally,
            CostFunc.Spend(Cost("1")),
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            divided_loyalties,
        ),
    ]

