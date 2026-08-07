from . import *

# Snitches Get Stitches

def GetAbilities() -> Sequence['Ability']:

    def snitches_get_stitches(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = this.GetBindFace()
        message.ReplaceTarget(face)

        def action(face: 'CardFace'):
            Faces.AddToVictoryDisplay([this, face], effect)

        message.IfThisAttackDefeats(
            face,
            action,
            effect
        )


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Venom", subtitle="Eddie Brock"),
            if_cannot_gain_surge=True,
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            Villain,
            snitches_get_stitches
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.Action,
        ).SetCost(Cost("2", same_type=True))
        .SetCostFunc(CostFunc.Exhaust(name="Venom")),
    ]

