from . import *

# * Professor X

def GetAbilities() -> Sequence['Ability']:

    def professor_x(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Confuse the villain",
                lambda targets: Faces.GiveStatus(targets, "Confused", effect)
            ).SetTarget(Villain, canbe_confused=True),
            AbilityFactory.ForChoiceAbility(
                "Stun a minion",
                lambda targets: Faces.GiveStatus(targets, "Stunned", effect)
            ).SetTarget(Minion),
            AbilityFactory.ForChoiceAbility(
                "Ready an X-MEN character",
                lambda targets: Faces.ReadyAll(targets, effect)
            ).SetTarget(Unit2, trait="X-MEN", canbe_ready=True)
        )

    return [
        AbilityFactory.DiscardThisWhenRoundEnd(
            AbilityType.ForcedResponse,
        ),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            "This",
            professor_x
        ),
    ]

