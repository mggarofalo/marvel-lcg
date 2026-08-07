from . import *

# Adder-tisement

def GetAbilities() -> Sequence['Ability']:

    def adder_tisement_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        faces = Worlds.GetEncounterDiscardPileCards(effect, CardFinder2("SERPENT SOCIETY", Minion))
        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            adder_tisement_revealed
        ),
    ]

