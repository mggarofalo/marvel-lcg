from . import *

# Sniper Shot

def GetAbilities() -> Sequence['Ability']:

    def sniper_shot_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        player = message.GetToPlayer()
        hero = player.GetHero()

        this.DealDamage([hero], 3, effect)

    def sniper_shot_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 3, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Hero",
            sniper_shot_hero
        ),
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            sniper_shot_alter_ego
        ),
    ]

