from . import *

# MojoMania

def GetAbilities() -> Sequence['Ability']:

    def mojomania(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="Wheel of Genres",
            card_type=Environment
        )


    return [
        AbilityFactory.SetModularSetsAside(lambda effect: 1 + 1 * Worlds.GetPlayerNumIcon(effect)),
        AbilityFactory.WhenCardSetup(
            "This",
            mojomania
        ),
    ]

