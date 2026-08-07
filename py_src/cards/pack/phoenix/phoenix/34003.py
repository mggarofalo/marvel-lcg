from . import *

# * Cyclops: Scott Summers

def GetAbilities() -> Sequence['Ability']:

    def cyclops_enter(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 2, 'power', effect)


    def cyclops_leave(effect: 'Effect', message: 'Message.WhenCardLeavePlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.RemoveCountersOn(effect.targets, 2, 'power', effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            cyclops_enter
        ).SetTarget(name="Phoenix Force"),
        AbilityFactory.WhenCardLeavePlay(
            AbilityType.ForcedInterrupt,
            "This",
            cyclops_leave
        ).SetTarget(name="Phoenix Force"),
    ]

