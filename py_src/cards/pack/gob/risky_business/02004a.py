from . import *

# Hostile Takeover - 1A

def GetAbilities() -> Sequence['Ability']:

    def hostile_takeover(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="Criminal Enterprise",
            card_type=Environment
        )

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            hostile_takeover
        )
    ]
