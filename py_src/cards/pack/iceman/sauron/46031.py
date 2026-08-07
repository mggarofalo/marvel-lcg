from . import *

# Life Drain

def GetAbilities() -> Sequence['Ability']:

    def life_drain(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetAgainstPlayer()
        identity = player.GetIdentity()

        identity.TakeDamage(this, 2, effect)
        Faces.GiveStatus([message.attacker], "Tough", effect)


    def life_drain_attach(face: 'CardFace', effect: 'Effect') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = face.CastTo(Minion)
        player = Worlds.GetCurrentPlayer(effect)
        if not face.DoActivate(player, effect):
            ThisCardGainSurge(effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_hp=True,
            if_cannot_gain_surge=True,
            when_attach_operation=life_drain_attach
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            life_drain
        ),
    ]

