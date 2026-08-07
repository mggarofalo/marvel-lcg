from . import *

# Nabbed!

def GetAbilities() -> Sequence['Ability']:

    def nabbed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        scheme = Worlds.FindCardOnField(effect, name="Abduct Superhumans", card_type=EncounterSideScheme)
        if not scheme:
            scheme = Find.FindAndPutIntoPlay(
                effect,
                player,
                name="Abduct Superhumans",
                card_type=EncounterSideScheme
            )

        ally = player.DiscardDeckUntil(
            effect,
            card_type=Ally
        )
        if ally and scheme:
            scheme.TuckCardUnderHere(ally, effect)
            value = ally.printed_cost.val
            this.PlaceThreatOnSchemes([scheme], value, effect)
            scheme.PlaceAccelerationToken(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            nabbed_revealed
        ),
    ]

