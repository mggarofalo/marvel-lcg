from . import *

# Edge of Reality

def GetAbilities() -> Sequence['Ability']:

    def edge_of_reality_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        ShuffleTopEncounterDeckIntoEachPlayersDeck(2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            edge_of_reality_revealed
        ),
    ]

