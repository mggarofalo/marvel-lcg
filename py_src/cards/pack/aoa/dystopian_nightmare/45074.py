from . import *

# Targeted for Extermination

def GetAbilities() -> Sequence['Ability']:

    def targeted_for_extermination(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            targeted_for_extermination,
            has_defeating_player=True
        ),
    ]

