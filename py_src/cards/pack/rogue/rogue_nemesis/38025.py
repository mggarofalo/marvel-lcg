from . import *

# * Mystique

def GetAbilities() -> Sequence['Ability']:

    def mystique(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
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
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.ForcedResponse,
            "This",
            "You",
            mystique
        ),
    ]

