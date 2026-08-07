from . import *

# * Deadpool

def GetAbilities() -> Sequence['Ability']:

    def deadpool(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        message.SetBeInstead(effect)
        this.SetHealth(1, effect)
        this.ChangeToForm(AlterEgo, effect)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            scheme.PlaceAccelerationToken(1, effect)


    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            deadpool
        ).SetName("The Regeneratin' Degenerate"),
    ]

