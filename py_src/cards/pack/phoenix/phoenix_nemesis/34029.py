from . import *

# * Dark Phoenix

def GetAbilities() -> Sequence['Ability']:

    def dark_phoenix_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            include_set_aside=True,
            name="Consume the World"
        )
        if face:
            face.Reveal(None, effect)

    return [
        AbilityFactory.PlaceThreatOnCardWhenThisScheme(
            AbilityType.NonKeywordStar,
            CardFinder(name="Consume the World"),
            if_able=True
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            dark_phoenix_revealed
        ),
    ]

