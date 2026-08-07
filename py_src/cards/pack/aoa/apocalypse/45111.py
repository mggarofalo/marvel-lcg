from . import *

# The Apocalypse Solution

def GetAbilities() -> Sequence['Ability']:

    def the_apocalypse_solution(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        villain = Worlds.FindCardOnField(
            effect,
            name="Apocalypse",
            card_type=Unit2
        )
        if villain:
            value = villain.printed_health_numeral
            Worlds.DiscardEncounterCards(value, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_apocalypse_solution,
        ),
    ]

