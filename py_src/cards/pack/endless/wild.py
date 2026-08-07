from . import *

# Wild

def GetAbilities() -> Sequence['Ability']:

    def wild(effect: 'Effect', message: 'Message.WhenGameInitialize') -> None:
        from engine.log import Notify
        from cards.database import CardsDB

        Notify.Game("Welcome to the WILD MODE")

        Notify.Game("Choose your first advanture")
        # name = player.AskChoosePaper(villain_ids)
        encounter_sets = CardsDB.GetPapers(set_name="Rhino")
        CardFactory.GenerateCards(encounter_sets, Worlds.GetEncounterDiscardPile(effect), effect.world)
        Faces.MoveAllTo(Worlds.GetEncounterDiscardPileCards(effect), Worlds.GetEncounterDeck(effect), effect)

    return [
        AbilityFactory.WhenGameInitialize(
            wild
        ),
    ]

