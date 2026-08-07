from . import *

# Martyr for Mutants

def GetAbilities() -> Sequence['Ability']:

    def martyr_for_mutants(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        player.DiscardDeckTopCards(9, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            martyr_for_mutants,
            has_defeating_player=True
        ),
    ]

