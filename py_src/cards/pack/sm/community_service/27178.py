from . import *

# Henchmen Heist

def GetAbilities() -> Sequence['Ability']:

    def henchmen_heist(effect: 'Effect', message: 'Message.AfterSchemeRemoveThreat') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = message.value
        this.PlaceThreatOnSchemes("MainScheme", value, effect)


    return [
        AbilityFactory.AfterSchemeRemoveThreat(
            AbilityType.ForcedResponse,
            "This",
            henchmen_heist
        ),
    ]

