from . import *

# The Crimson Cowl - 1A

def GetAbilities() -> Sequence['Ability']:

    def the_crimson_cowl(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="Ultron Drones",
            card_type=Environment,
        )

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            the_crimson_cowl
        ),
    ]
