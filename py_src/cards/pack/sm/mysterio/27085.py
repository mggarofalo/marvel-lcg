from . import *

# * Mysterio

def GetAbilities() -> Sequence['Ability']:

    def mysterio_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        ShuffleTopEncounterDeckIntoEachPlayersDeck(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mysterio_revealed
        ),
        Mysterio(2)
    ]

