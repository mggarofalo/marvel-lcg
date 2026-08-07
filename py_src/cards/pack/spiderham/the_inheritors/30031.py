from . import *

# * Bora

def GetAbilities() -> Sequence['Ability']:

    def bora_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        if Worlds.FindCardOnField(effect, CardFinder2("WEB-WARRIOR", Unit2)):
            this.PlaceThreatOnSchemes("EachScheme", 1, effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("INHERITOR", Minion),
            acceleration_icon=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            bora_revealed
        ),
    ]

