from . import *

# New World Hydra

def GetAbilities() -> Sequence['Ability']:

    def new_world_hydra_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        RevealSideSchemeDeckTopCard(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            new_world_hydra_revealed
        )
    ]

