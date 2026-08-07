from . import *

# The Rule of Magnus A

def GetAbilities() -> Sequence['Ability']:

    def the_rule_of_magnus_a_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.PlaceCountersOn([this], 2, 'magnet', effect)

        magneto = Worlds.FindCardOnField(
            effect,
            name="Magneto",
        )
        if not magneto or \
            not magneto.GetInventoryDeck().FindCards(name="Physical Strain"):
            player = Worlds.GetFirstPlayer(effect)

            face = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                trait="MAGNETIC",
                card_type=Attachment
            )

            if face:
                face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_rule_of_magnus_a_revealed
        ),
    ]

