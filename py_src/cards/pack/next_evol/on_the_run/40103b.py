from . import *

# Gotta Get Away

def GetAbilities() -> Sequence['Ability']:

    def gotta_get_away_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        def action(player: 'Player'):
            minion = Search.EncounterCard(
                effect,
                player,
                include_discard_pile=False,
                trait="MARAUDER",
                card_type=Minion,
            )

            if minion:
                minion.PutIntoPlay(player, effect)

        Players.ForEachPlayer(effect, action)


    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("MARAUDER", Minion),
            steady=1,
        ),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Villain,
            steady=1,
            expert_mode_only=True,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            gotta_get_away_revealed
        ),
    ]

