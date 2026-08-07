from . import *

# * Victoria Hand

def GetAbilities() -> Sequence['Ability']:

    def victoria_hand(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            victoria_hand
        ).SetTarget(Support, trait="S.H.I.E.L.D"),
    ]

