from . import *

# Establish Perimeter

def GetAbilities() -> Sequence['Ability']:

    def establish_perimeter(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(PlayerSideScheme)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        Faces.GiveStatus(units, "Tough", effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            establish_perimeter,
        ),
    ]

