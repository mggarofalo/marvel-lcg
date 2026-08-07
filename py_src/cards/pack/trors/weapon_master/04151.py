from . import *

# Concussion Grenade

def GetAbilities() -> Sequence['Ability']:

    def concussion_grenade_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetAlterEgo()
        value = 1
        if identity.IsConfused():
            value = 2
        Faces.GiveStatus([identity], "Confused", effect)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)

    def concussion_grenade_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetHero()
        value = 1
        if identity.IsStunned():
            value = 2
        Faces.GiveStatus([identity], "Stunned", effect)
        this.DealDamage([identity], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            concussion_grenade_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            concussion_grenade_hero
        ),
    ]

