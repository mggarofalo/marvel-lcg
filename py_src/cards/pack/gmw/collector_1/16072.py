from . import *

# * Collector

def GetAbilities() -> Sequence['Ability']:

    def collector_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        def action(player: 'Player'):
            PutTopCardIntoTheCollection(player, effect)

        Players.ForEachPlayer(effect, action)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            value = GetTheCollection(effect).deck.GetSize()
            this.PlaceThreatOnSchemes([scheme], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            collector_revealed
        ),
        PutCardIntoTheCollectionInsteadDiscard(True)
    ]

