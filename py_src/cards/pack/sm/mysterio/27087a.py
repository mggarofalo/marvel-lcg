from . import *
from cards.pack.sm.campaign import *

# Maze of Mirrors

def GetAbilities() -> Sequence['Ability']:

    def maze_of_mirrors(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        encounter_deck = Worlds.GetEncounterDeck(effect)
        def action(player: 'Player'):
            face = encounter_deck.FindCard(
                name="Shifting Apparition",
                card_type=Minion,
            )
            if face:
                face.PutIntoPlay(player, effect)

        Players.ForEachPlayer(effect, action)

    def campaign_expert(effect: 'Effect', message: 'Message.WhenCampaignSetup'):
        ShuffleTopEncounterDeckIntoEachPlayersDeck(2, effect)

    return [
        *CampaignSetup(3, campaign_expert=campaign_expert),
        AbilityFactory.WhenCardSetup(
            "This",
            maze_of_mirrors
        ),
    ]

