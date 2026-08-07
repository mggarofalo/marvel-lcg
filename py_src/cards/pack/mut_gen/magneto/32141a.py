from . import *

# Asteroid M A

def GetAbilities() -> Sequence['Ability']:

    def asteroid_m(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.Reveal(
            effect,
            name="Boarding Party",
            card_type=SchemeSide2
        )

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            asteroid_m
        ),
    ]

