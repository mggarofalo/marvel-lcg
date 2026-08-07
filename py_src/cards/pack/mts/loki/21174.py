from . import *

# Devious Sorcery

def GetAbilities() -> Sequence['Ability']:

    def devious_sorcery_alter_ego(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsStunned():
            this.PlaceThreatOnSchemes("MainScheme", 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)

    def devious_sorcery_hero(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()
        if identity.IsStunned():
            identity.TakeDamage(this, 2, effect)
        else:
            Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.WhenThisRevealed(
            "Alter-Ego",
            devious_sorcery_alter_ego
        ),
        AbilityFactory.WhenThisRevealed(
            "Hero",
            devious_sorcery_hero
        ),
    ]

