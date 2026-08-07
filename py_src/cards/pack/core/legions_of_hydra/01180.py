from . import *

# Legions of Hydra

def GetAbilities() -> Sequence['Ability']:

    def legions_of_hydra_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()

        face = Worlds.FindCardOnField(
            effect,
            name="Madame Hydra",
            card_type=Minion
        )

        if not face:
            face = Search.EncounterCard(
                effect,
                include_discard_pile=True,
                name="Madame Hydra",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(player, effect)

        value = 2 * Worlds.FindCardSizeOnField(effect, CardFinder2("HYDRA", Enemy))
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            legions_of_hydra_revealed
        ),
    ]

