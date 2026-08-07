from . import *

# * Throg: Puddlegulp

def GetAbilities() -> Sequence['Ability']:

    def throg(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            throg,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().engaged_minions.GetSize() != 0,
            ]
        ).SetTarget("Trigger"),
    ]

