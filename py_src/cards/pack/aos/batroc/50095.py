from . import *

# Commandeer Security Office

def GetAbilities() -> Sequence['Ability']:

    def commandeer_security_office_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        if IfAlertLevelIsHighSide(effect):
            this.PlaceAccelerationToken(1, effect)

    def commandeer_security_office(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        env = GetAlertLevel(effect)
        if env:
            Faces.RemoveTokensOn([env], "1*", 'threat', effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            commandeer_security_office_revealed
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            commandeer_security_office,
        ),
    ]

