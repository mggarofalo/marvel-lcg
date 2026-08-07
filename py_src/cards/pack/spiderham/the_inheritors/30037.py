from . import *

# * Solus

def GetAbilities() -> Sequence['Ability']:

    def solus_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            Faces.GiveFacedownBoostCards([this], 1, effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            villainous=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            solus_revealed
        ),
    ]

