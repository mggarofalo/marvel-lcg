from . import *

# Knife Fight

def GetAbilities() -> Sequence['Ability']:

    def knife_fight_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        enemy = Filter.One(Worlds.GetOnFieldEnemies(effect), effect, highest_atk=True)
        hero = player.GetHero()
        if enemy:
            enemy = enemy
            hero.TakeDamage(this, enemy.attack, effect)
            this.DealDamage([enemy], hero.attack, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            ThisCardGainSurge
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            knife_fight_hero
        ),
    ]

