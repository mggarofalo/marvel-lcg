from . import *

# The Sentinel Factory B

def GetAbilities() -> Sequence['Ability']:

    def the_sentinel_factory(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

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
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            CardFinder2("SENTINEL", Minion),
            guard=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            the_sentinel_factory
        ),
    ]

