from . import *

# * Deadpool: Wade Wilson

def GetAbilities() -> Sequence['Ability']:

    def deadpool(effect: 'Effect', message: 'Message.WhenUnitWouldBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.SetBeInstead(effect)
        this.HealHealth(3, effect)
        scheme = Worlds.FindMainScheme(effect)
        if scheme:
            scheme.PlaceAccelerationToken(1, effect)


    return [
        AbilityFactory.WhenUnitWouldBeDefeated(
            AbilityType.ForcedResponse,
            "This",
            deadpool,
            by_consequential_damage=True,
        ),
    ]

