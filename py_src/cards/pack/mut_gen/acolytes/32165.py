from . import *

# The Acolytes

def GetAbilities() -> Sequence['Ability']:

    def the_acolytes_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        minions = Worlds.GetEncounterDiscardPile(effect).FindCards(trait="ACOLYTE", card_type=Minion)
        Faces.ShuffleAllTo(minions, "EncounterDeck", effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("ACOLYTE", Minion),
            guard=1,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_acolytes_boost
        ),
    ]

