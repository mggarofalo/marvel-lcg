from . import *

# Making Connections

def GetAbilities() -> Sequence['Ability']:

    def making_connections(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        AsideModularSet.ChooseRandom(effect, do_shuffle=True)

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            making_connections
        ),
        AbilityFactory.SetModularSetsAside(7)
    ]

