from . import *
from cards.pack.trors.campaign import *

# Hunting Down Heroes

def GetAbilities() -> Sequence['Ability']:

    def hunting_down_heroes(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        villain = Worlds.FindVillain(effect)
        assert villain
        face = Search.SearchForCard(
            effect,
            first_player,
            include_encounter_deck=True,
            name="Hydra Patrol",
            card_type=SchemeSide2,
        )
        if face:
            face.PutIntoPlay(first_player, effect)


    return [
        *CampaignSetup(3),
        AbilityFactory.WhenCardSetup(
            "This",
            hunting_down_heroes
        )
    ]

