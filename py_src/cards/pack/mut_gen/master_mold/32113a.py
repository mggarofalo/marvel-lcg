from . import *

# Master Mold's Agenda A

def GetAbilities() -> Sequence['Ability']:

    def master_molds_agenda_a_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Worlds.GetEncounterDeck(effect).ShuffleWithDiscardPile(False, effect)

        def action(player: 'Player'):
            minion = Worlds.DiscardEncounterCardsUntil(
                effect,
                card_type=Minion,
                trait="SENTINEL",
            )
            if minion:
                minion.PutIntoPlay(player, effect)
        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            master_molds_agenda_a_revealed
        ),
    ]

