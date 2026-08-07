from . import *

# * Nick Fury

def GetAbilities() -> Sequence['Ability']:

    def nick_fury(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'all-purpose', effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.Response,
            "This",
            nick_fury
        ).SetTarget(Support, trait="S.H.I.E.L.D"),
    ]

