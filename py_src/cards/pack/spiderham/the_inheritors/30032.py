from . import *

# * Brix

def GetAbilities() -> Sequence['Ability']:

    def brix_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            patrol=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            brix_revealed
        ),
    ]

