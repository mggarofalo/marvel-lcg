from . import *

# * Batroc

def GetAbilities() -> Sequence['Ability']:

    def batroc(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        env = GetAlertLevel(effect)
        if env:
            Faces.PlaceTokensOn([env], 1, 'threat', effect)

    def batroc_defeated(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        message.SetBeInstead(effect)

        this.ResetHealth(effect, 8)
        this.RemoveThreatFromSchemes("MainScheme", 6, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            batroc
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            batroc_defeated
        ),
    ]

