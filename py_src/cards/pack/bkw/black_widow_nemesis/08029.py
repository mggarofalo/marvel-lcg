from . import *

# Deadly Shot

def GetAbilities() -> Sequence['Ability']:

    def deadly_shot_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True)
        this.PlaceThreatOnSchemes("MainScheme", 1, effect)

    def deadly_shot_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True)
        player.GetIdentity().TakeDamage(this, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            deadly_shot_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            deadly_shot_hero
        ),
    ]

