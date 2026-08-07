from . import *

# * Belladonna

def GetAbilities() -> Sequence['Ability']:

    def belladonna(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 2, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            belladonna
        ),
    ]

