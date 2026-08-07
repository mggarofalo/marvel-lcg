from . import *

# The Ravages of War

def GetAbilities() -> Sequence['Ability']:

    def the_ravages_of_war(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        player.DiscardControlCards(effect, upgrade=True, support=True)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_ravages_of_war,
            has_defeating_player=True
        ),
    ]

