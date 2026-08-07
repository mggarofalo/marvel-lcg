from . import *

# Technological Innovation

def GetAbilities() -> Sequence['Ability']:

    def technological_innovation_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            trait="TECH",
            card_type=Attachment
        )
        if face:
            face.Reveal(player, effect)

        value = len(Worlds.FindCardsOnField(effect, CardFinder2("TECH")))
        this.PlaceThreatOnSchemes([this], value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            technological_innovation_revealed
        ),
    ]

