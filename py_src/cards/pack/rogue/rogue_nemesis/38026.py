from . import *

# Mystique's Manipulations

def GetAbilities() -> Sequence['Ability']:

    def mystiques_manipulations(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetDefeatingPlayer()

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            include_set_aside=True,
            name="Misled",
            card_type=Treachery
        )
        if face:
            Faces.ShuffleAllTo([face], player.player_deck, effect)

    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            mystiques_manipulations,
            has_defeating_player=True
        ),
    ]

