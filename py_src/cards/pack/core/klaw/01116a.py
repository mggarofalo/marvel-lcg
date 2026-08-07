from . import *

# Underground Distribution - 1A

def GetAbilities() -> Sequence['Ability']:

    def underground_distribution(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.Reveal(
            effect,
            name="Defense Network",
            card_type=SchemeSide2,
        )

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            underground_distribution
        )
    ]
