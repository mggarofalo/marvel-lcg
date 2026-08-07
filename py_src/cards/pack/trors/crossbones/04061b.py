from . import *

# Attack on Mount Athena

def GetAbilities() -> Sequence['Ability']:

    def attack_on_mount_athena_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        RevealExperimentalWeaponsDeckTopCard(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            attack_on_mount_athena_revealed
        )
    ]

