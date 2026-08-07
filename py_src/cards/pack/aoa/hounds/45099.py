from . import *

# Ahab's Energy Spear

def GetAbilities() -> Sequence['Ability']:

    def ahabs_energy_spear(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)
        message.GainPiercing(effect)

        def action():
            Faces.DiscardAll([this], effect)

        RunAt.AfterEnemyActivationEnd(effect, message, action)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Ahab"),
            if_cannot_attach_to=Villain
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            ahabs_energy_spear,
        ),
    ]

