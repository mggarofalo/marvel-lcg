from . import *

# * Jean Grey

def GetAbilities() -> Sequence['Ability']:

    def jean_grey(effect: 'Effect', faces: List['CardFace']) -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.PlaceCountersOn(faces, 4, 'power', effect)

    def jean_grey_response(effect: 'Effect', message: 'Message.AfterUnitRecovery') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'power', effect)


    return [
        AbilityFactory.SetupPutIntoPlay(
            ["34002a,34002b"],
            operation=jean_grey,
        ),
        AbilityFactory.AfterUnitMakeRecovery(
            AbilityType.Response,
            "You",
            jean_grey_response,
        ).SetTarget(name="Phoenix Force"),
    ]

