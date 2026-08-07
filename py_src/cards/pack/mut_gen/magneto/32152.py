from . import *

# Electric Shock

def GetAbilities() -> Sequence['Ability']:

    def electric_shock_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Confused", effect)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            value = scheme.GetCounters('magnet')
            this.PlaceThreatOnSchemes([scheme], value, effect)

    def electric_shock_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)

        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            value = scheme.GetCounters('magnet')
            identity.TakeDamage(this, value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            electric_shock_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            electric_shock_hero
        ),
    ]

