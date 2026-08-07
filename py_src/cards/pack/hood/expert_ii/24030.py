from . import *

# Ruination

def GetAbilities() -> Sequence['Ability']:

    def ruination_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=SchemeSide2,
        )
        if face:
            face.Reveal(player, effect)

        this.PlaceThreatOnSchemes("EachScheme", 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            ruination_revealed
        ),
    ]

