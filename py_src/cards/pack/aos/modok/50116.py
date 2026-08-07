from . import *

# Nanobots

def GetAbilities() -> Sequence['Ability']:

    def nanobots(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        this.HealthUnits([message.trigger], 1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            MODOK_FINDER
        ),
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            MODOK_FINDER,
            nanobots
        ),
        AfterMODOKHitPointsAreResetDiscardThisCard()
    ]

