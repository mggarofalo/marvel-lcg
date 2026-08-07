from . import *

# Welcome to Murderworld

def GetAbilities() -> Sequence['Ability']:

    def welcome_to_murderworld(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        identity = player.GetIdentity()
        identity.TakeDamage(this, 2, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            welcome_to_murderworld,
            has_defeating_player=True
        ),
    ]

