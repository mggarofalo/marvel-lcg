from . import *

# The Assassins Guild

def GetAbilities() -> Sequence['Ability']:

    def the_assassins_guild(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.PlaceThreatOnSchemes([this], 2, effect)

    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            CardFinder2("ASSASSIN", Minion),
            None,
            the_assassins_guild
        ),
    ]

