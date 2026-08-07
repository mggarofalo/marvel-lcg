from . import *

# Fanaticism

def GetAbilities() -> Sequence['Ability']:

    def fanaticism(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)
        message.GainPiercing(effect)

        def action():
            Faces.RemoveCountersOn([this], 1, 'fury', effect)

        RunAt.AfterEnemyActivationEnd(effect, message, action)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Ronan the Accuser"),
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Ronan the Accuser"),
            fanaticism,
        ),
    ]

