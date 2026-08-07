from . import *

# * Sabretooth

def GetAbilities() -> Sequence['Ability']:

    def sabretooth(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        face = Worlds.DiscardEncounterTopCard(effect)
        if face:
            value = FacesCounter.CountTotalBoostIcons([face])
            this.HealthUnits([this], value, effect)


    return [
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            sabretooth,
        ),
    ]

