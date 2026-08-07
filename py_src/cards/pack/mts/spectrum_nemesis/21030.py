from . import *

# Radioactive Blast

def GetAbilities() -> Sequence['Ability']:

    def radioactive_blast_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 2, effect)

    def radioactive_blast_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        player.GetIdentity().TakeDamage(this, 2, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            radioactive_blast_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            radioactive_blast_hero
        ),
    ]

