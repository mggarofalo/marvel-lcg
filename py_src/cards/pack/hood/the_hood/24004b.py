from . import *

# Making Connections

def GetAbilities() -> Sequence['Ability']:

    def making_connections_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        EachPlayerMustResolveFoulPlayAbility(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            making_connections_revealed
        ),
    ]

