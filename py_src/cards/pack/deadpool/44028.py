from . import *

# * Git Gud

def GetAbilities() -> Sequence['Ability']:

    def git_gud(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)
        identity = message.trigger.CastTo(Identity)
        identity.SetHealth(1, effect)
        identity.ChangeToForm(AlterEgo, effect)

        Faces.RemoveAllFromGame([this], effect)


    return [
        # Reduce the cost to play Git Gud by 2 if you did not win your previous game of Marvel Champions.
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            Identity,
            git_gud
        )
    ]

