from . import *

# Gauntlet Beam

def GetAbilities() -> Sequence['Ability']:

    def gauntlet_beam_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.ExhaustAll([identity], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            Villain,
            piercing=True,
            ranged=True
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("RRR")),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            gauntlet_beam_boost
        ),
    ]

