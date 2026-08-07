from . import *

# Psionic Machetes

def GetAbilities() -> Sequence['Ability']:

    def psionic_machetes_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.would_atk_message.GainPiercing(effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            MODOK_FINDER
        ),
        AbilityFactory.UnitAttackGainKeyword(
            MODOK_FINDER,
            piercing=True,
        ),
        AfterMODOKHitPointsAreResetDiscardThisCard(),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            psionic_machetes_boost,
            during_attack=True,
        ),
    ]

