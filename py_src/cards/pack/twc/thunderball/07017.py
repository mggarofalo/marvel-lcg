from . import *

# * Thunderball (I)

def GetAbilities() -> Sequence['Ability']:

    def thunderball(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        characters = message.attacked.GetControlByPlayer().GetControlCharacters()
        this.DealDamage(characters, 1, effect)


    return [
        PlaceTheThreatOnHisSideScheme(),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            "This",
            "You",
            thunderball
        )
    ]

