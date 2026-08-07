from . import *

# Thrown Object

def GetAbilities() -> Sequence['Ability']:

    def thrown_object(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.DiscardAll([this], effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.UnitAttackGainKeyword(
            "AttachedVillain",
            ranged=True
        ),
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            Villain,
            thrown_object
        ),
    ]

