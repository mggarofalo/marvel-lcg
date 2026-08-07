from . import *

# Telepathic Trickery

def GetAbilities() -> Sequence['Ability']:

    def telepathic_trickery(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        identity = initiator.GetIdentity()

        this.RemoveThreatFromSchemes(effect.targets, 4, effect)

        if identity.HasTrait("UNLEASHED"):
            def action(targets: Sequence['CardFace']):
                Faces.GiveStatus(targets, "Stunned", effect)
                Faces.GiveStatus(targets, "Confused", effect)

            initiator.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    action
                ).SetTarget(Enemy, canbe_status=["Stunned", "Confused"])
            )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            telepathic_trickery,
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Enemy, canbe_status=["Stunned", "Confused"],
            condition=lambda effect:
                effect.GetInitiator().GetIdentity().HasTrait("UNLEASHED")
        ),
    ]

