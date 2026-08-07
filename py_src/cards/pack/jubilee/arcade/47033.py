from . import *

# Hall of Mirrors

def GetAbilities() -> Sequence['Ability']:

    def hall_of_mirrors(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        identity = player.GetIdentity()
        if identity.IsConfused():
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        else:
            Faces.GiveStatus([identity], "Confused", effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            hall_of_mirrors,
            has_defeating_player=True
        ),
    ]

