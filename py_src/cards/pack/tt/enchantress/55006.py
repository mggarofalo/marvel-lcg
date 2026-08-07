from . import *

# Future of Despair

def GetAbilities() -> Sequence['Ability']:

    def future_of_despair_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.FindCardsOnField(effect, CardFinder(trait="ENCHANTMENT"))
        Faces.PlaceCountersOn(faces, 1, 'charm', effect)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder(name="Enchantress"),
            stalwart=1,
        ),
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            CardFinder(name="Enchantress"),
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            future_of_despair_revealed
        ),
    ]

