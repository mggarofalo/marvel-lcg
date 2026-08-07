from . import *

# Gene Therapy

def GetAbilities() -> Sequence['Ability']:

    def gene_therapy(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.GainOverKill(effect)
        message.GainPiercing(effect)

        RunAt.AfterEnemyActivationEnd(effect, message, lambda: Faces.DiscardAll([this], effect))


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Enemy,
            lowest_atk=True,
            without_another_copy=True,
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "AttachedEnemy",
            gene_therapy
        ),
    ]

