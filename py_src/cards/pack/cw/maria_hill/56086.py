from . import *

# S.H.I.E.L.D. Helicarrier

def GetAbilities() -> Sequence['Ability']:

    def shield_helicarrier_defeated(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        identity = player.GetIdentity()
        identity.DealDamage(player.GetControlCharacters(), 1, effect)
        identity.DealDamage(player.GetEngagedMinions(), 1, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            shield_helicarrier_defeated,
        ),
    ]

