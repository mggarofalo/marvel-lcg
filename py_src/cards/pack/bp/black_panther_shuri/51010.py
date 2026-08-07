from . import *

# Kimoyo Beads

def GetAbilities() -> Sequence['Ability']:

    def kimoyo_beads(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        this.RemoveThreatFromSchemes(effect.targets, 1, effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard this card to confuse an enemy",
                lambda targets:
                    Faces.GiveStatus(targets, "Confused", effect),
            ).SetTarget(Enemy, canbe_confused=True)
            .SetCostFunc(CostFunc.Discard("This"))
        )


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            kimoyo_beads
        ).SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

