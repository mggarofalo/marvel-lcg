from . import *

# Mana Drain

def GetAbilities() -> Sequence['Ability']:

    def mana_drain_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        res = player.DeclareResourceType(g=False)

        def action(player: 'Player'):
            player.DrawUp(2, effect)

        Players.ForEachPlayer(effect, action)

        def action2(player: 'Player'):
            player.DiscardHandCards("All", effect, finder=CardFinder(has_printed_res=res))

        Players.ForEachPlayer(effect, action2)

    def mana_drain_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            mana_drain_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            mana_drain_boost
        ),
    ]

