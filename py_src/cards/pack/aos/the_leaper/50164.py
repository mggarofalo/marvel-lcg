from . import *

# Parcours du Combattant

def GetAbilities() -> Sequence['Ability']:

    def parcours_du_combattant_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        from cards.pack.aos.thunderbolts import EachPlayerEngagesEachMinionEngagedWithThePlayerClockwiseFromThem
        EachPlayerEngagesEachMinionEngagedWithThePlayerClockwiseFromThem(effect)

    def parcours_du_combattant_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        def action():
            minions = Worlds.GetOnFieldMinions(effect, CardFinder(not_engaged_with=player))
            minion = Filter.One(minions, effect)
            if minion:
                minion.EngagePlayer(player, effect)
        RunAt.AfterResolveMessage(effect, message.would_atk_message, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            parcours_du_combattant_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            parcours_du_combattant_boost,
            during_attack=True
        ),
    ]

