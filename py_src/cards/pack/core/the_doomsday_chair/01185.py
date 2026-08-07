from . import *

# Biomechanical Upgrades

def GetAbilities() -> Sequence['Ability']:

    def biomechanical_upgrades(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        message.SetBeInstead(effect)
        this.HealthUnits([message.trigger], "All", effect)

        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            highest_printed_hp=True,
            without_another_copy=True
        ),
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "AttachedMinion",
            biomechanical_upgrades
        ),
    ]

