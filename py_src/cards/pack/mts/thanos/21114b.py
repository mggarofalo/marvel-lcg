from . import *

# The Infinity Stones

def GetAbilities() -> Sequence['Ability']:

    def the_infinity_stones_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        face = GetInfinityStone(effect).GetTopCard()
        if face:
            face.PutIntoPlay("FirstPlayer", effect)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=False,
            name="Sanctuary",
            card_type=SchemeSide2
        )
        if face:
            face.Reveal(None, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_infinity_stones_revealed
        ),
    ]

