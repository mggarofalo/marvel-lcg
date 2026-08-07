from . import *

# Reactor Meltdown

def GetAbilities() -> Sequence['Ability']:

    def reactor_meltdown(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        this.DealDamage(Worlds.GetOnFieldFriendlyCharacters(effect), 1, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            reactor_meltdown,
        ),
    ]

