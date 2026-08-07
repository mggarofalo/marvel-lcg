from . import *

# A Time of Famine

def GetAbilities() -> Sequence['Ability']:

    def a_time_of_famine(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()
        player.DiscardDeckTopCards(10, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            a_time_of_famine,
            has_defeating_player=True
        ),
    ]

