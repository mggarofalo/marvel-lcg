from . import *

# * Speed: Thomas Shepherd

def GetAbilities() -> Sequence['Ability']:

    def speed(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.Response,
            "This",
            speed
        ).SetTarget("Trigger")
        .LimitOncePerRound(),
    ]

