from . import *

# * Senyaka

def GetAbilities() -> Sequence['Ability']:

    def senyaka(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetKillerPlayer()
        if player:
            identity = player.GetIdentity()
            if identity.IsStunned():
                identity.TakeDamage(this, 3, effect)
            else:
                Faces.GiveStatus([identity], "Stunned", effect)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True,
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            senyaka
        ),
    ]

