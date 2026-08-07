from . import *

# * Mister Sinister

def GetAbilities() -> Sequence['Ability']:

    def mister_sinister_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        if this.GetInventoryDeck().FindCardSize(CardFinder2("SUPERPOWER", Attachment)) < 2:
            value = "3*"
        else:
            value = "2*"
        this.PlaceThreatOnSchemes("MainScheme", value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mister_sinister_revealed
        ),
        MisterSinister(3)
    ]

