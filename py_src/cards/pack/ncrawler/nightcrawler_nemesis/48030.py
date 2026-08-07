from . import *

# Brimstone Strike

def GetAbilities() -> Sequence['Ability']:

    def brimstone_strike_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        enemy = Find.FindAndReveal(
            effect,
            player,
            name="Azazel",
            card_type=Enemy,
        )
        if enemy:
            enemy.DoSchemes(player, effect)

    def brimstone_strike_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        Find.FindAndReveal(
            effect,
            player,
            name="Azazel",
        )

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            brimstone_strike_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            brimstone_strike_hero
        ),
    ]

