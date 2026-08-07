from . import *

# Bull Rush

def GetAbilities() -> Sequence['Ability']:

    def bull_rush_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = GetBulldozer(effect)

        def action(placed_threat: int):
            player.DiscardDeckTopCards(placed_threat, effect)
        villain.DoSchemes(player, effect, for_each_threat_placed=action)

    def bull_rush_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        villain = GetBulldozer(effect)
        villain.DoAttackYou(player, effect,
            if_this_attack_deals_damage=lambda damage:
                player.DiscardDeckTopCards(damage, effect)
            )


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            bull_rush_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            bull_rush_hero
        )
    ]

