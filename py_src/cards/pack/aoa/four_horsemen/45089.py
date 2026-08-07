from . import *

# The Specter of Death

def GetAbilities() -> Sequence['Ability']:

    def the_specter_of_death(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        this.DealDamage(player.GetControlCharacters(), 1, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_specter_of_death,
            has_defeating_player=True
        ),
    ]

