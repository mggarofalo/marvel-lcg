from . import *

# Size Increase

def GetAbilities() -> Sequence['Ability']:

    def size_increase(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'size', effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "AttachedEnemy",
            size_increase
        ),
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Yellowjacket"),
            if_cannot_attach_to=Villain
        ),
    ]

