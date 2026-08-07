from . import *

# * Jennix

def GetAbilities() -> Sequence['Ability']:

    def jennix_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            Faces.GiveStatus([this], "Tough", effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            guard=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            jennix_revealed
        ),
    ]

