from . import *

# Psionic Illusion

def GetAbilities() -> Sequence['Ability']:

    def psionic_illusion(effect: 'Effect', message: 'Message.WhenUnitWouldAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.attacker.GetControlByPlayer()
        res_type = player.DeclareResourceType()
        face = player.DiscardDeckTopCard(effect)
        res = FacesCounter.GetPrintedResources([face])

        if not res.HasColorPrinted(res_type):
            def action(targets: Sequence['CardFace']):
                assert len(targets) == 1
                assert Unit2.IsType(targets[0])
                message.ChangeTarget(targets[0], effect)
                Faces.DiscardAll([this], effect)

            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    action
                ).SetTarget(Friend)
            )


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.WhenUnitWouldAttackUnit(
            AbilityType.ForcedInterrupt,
            "You",
            Enemy,
            psionic_illusion
        ),
    ]

