from . import *

# * Cable

def GetAbilities() -> Sequence['Ability']:

    def cable(effect: 'Effect', message: 'Message.AfterUnitDefeatedScheme') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        # Hack, because of `LimitOncePerPhase`
        AbilityFactory.AfterUnitDefeatSchemeInternal(
            AbilityType.Response,
            None,
            cable,
            who_make_thwart="This",
        ).SetTarget("Trigger", canbe_ready=True)
        .LimitOncePerPhase(),
    ]

