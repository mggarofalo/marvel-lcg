from . import *

# Arcade's Funhouse

def GetAbilities() -> Sequence['Ability']:

    def arcades_funhouse(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        identity = player.GetIdentity()

        if identity.IsStunned():
            player.DiscardControlCards(effect, ally=True, upgrade=True)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            arcades_funhouse,
            has_defeating_player=True
        ),
    ]

