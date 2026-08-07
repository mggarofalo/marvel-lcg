from . import *

# Melee in the Mojo-seum

def GetAbilities() -> Sequence['Ability']:

    def melee_in_the_mojo_seum_revealed(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="The Champion",
            card_type=Environment
        )
        SetupCards.PutIntoPlay(
            effect,
            name="The Challengers",
            card_type=Environment
        )

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            melee_in_the_mojo_seum_revealed
        ),
    ]

