from . import *

# Rise from the Ashes

def GetAbilities() -> Sequence['Ability']:

    def rise_from_the_ashes(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)

        Faces.ReadyAll(effect.targets, effect)
        this.HealthUnits(effect.targets, "Printed", effect)

        face = Worlds.FindCardOnField(
            effect,
            name="Phoenix Force",
        )
        if face:
            Faces.RemoveCountersOn([face], "All", 'power', effect)


    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.Interrupt,
            "You",
            rise_from_the_ashes
        ).SetCostFunc(CostFunc.RemoveFromGame("This"))
        .SetTarget("YourIdentity"),
    ]

