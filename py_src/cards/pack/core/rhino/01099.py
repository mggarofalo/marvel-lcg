from . import *

# Charge

def GetAbilities() -> Sequence['Ability']:

    def charge(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)

        def disacard_this() -> None:
            Faces.DiscardAll([this], effect)

        RunAt.AfterEnemyActivationEnd(effect, message, disacard_this)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Rhino")
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            CardFinder(name="Rhino"),
            charge
        )
    ]
