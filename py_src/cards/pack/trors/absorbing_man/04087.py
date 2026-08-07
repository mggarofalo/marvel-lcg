from . import *

# Steel Kick

def GetAbilities() -> Sequence['Ability']:

    def steel_kick_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        if AbsorbingManHasTrait(effect, 'METAL'):
            damage = 4
        else:
            damage = 3
        player.GetIdentity().TakeIndirectDamage(this, damage, effect)


    def steel_kick_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        if AbsorbingManHasTrait(effect, 'METAL'):
            value = 3
        else:
            value = 2
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            steel_kick_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            steel_kick_hero
        ),
    ]

