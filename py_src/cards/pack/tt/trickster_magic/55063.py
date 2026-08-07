from . import *

# * Absorbing Man: Carl Creel

def GetAbilities() -> Sequence['Ability']:

    def absorbing_man(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldThwart') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        def action(targets: Sequence['CardFace']):
            Faces.ExhaustAll(targets, effect)
            face = targets[0]
            if HasCost.IsType(face):
                value = min(3, face.printed_cost.val)
            else:
                value = 0
            if isinstance(message, Message.WhenUnitWouldAttack):
                message.GainATKForThisAttack(value, effect)
            else:
                message.GainTHWForThisThwart(value, effect)

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "",
                action,
            ).SetTarget("Upgrade|Support", canbe_exhaust=True)
        )


    return [
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.WhenUnitWouldAttackOrThwart(
            AbilityType.Interrupt,
            "This",
            absorbing_man
        ),
    ]

