from . import *

# Guild Assassin

def GetAbilities() -> Sequence['Ability']:

    def guild_assassin(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            "This",
            None,
            guild_assassin
        ),
    ]

