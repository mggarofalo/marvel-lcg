from . import *

# Escaping with Hope

def GetAbilities() -> Sequence['Ability']:

    def escaping_with_hope_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            minion = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=True,
                trait="MARAUDER",
                card_type=Minion,
            )

            if minion:
                minion.PutIntoPlay(player, effect)

        Players.ForEachPlayer(effect, action)

        minions = Worlds.GetOnFieldEnemies(
            effect,
            CardFinder(
            trait="MARAUDER")
        )
        Faces.GiveStatus(minions, "Tough", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            escaping_with_hope_revealed
        ),
    ]

