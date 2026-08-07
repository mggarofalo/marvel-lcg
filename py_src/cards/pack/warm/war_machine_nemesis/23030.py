from . import *

# Deadly Light Show

def GetAbilities() -> Sequence['Ability']:

    def deadly_light_show(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        units = Worlds.GetPlayersIdentities(effect)
        this.DealDamage(units, 1, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            deadly_light_show,
        ),
    ]

