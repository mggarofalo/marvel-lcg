from . import *

# Mental Transferal

def GetAbilities() -> Sequence['Ability']:

    def mental_transferal(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        face = this.GetBindFace()
        if CanHealth.IsType(face):
            face.TakeDamage(this, message.took_damage, effect)

        Faces.DiscardAll([this], effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Hope Summers"),
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="Stryfe's Grasp"
                    ) != None
            ],
        ),
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
            conditions=[
                lambda effect, message:
                    Worlds.FindCardOnField(
                        effect,
                        name="Stryfe's Grasp"
                    ) == None
            ]
        ),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            CardFinder(name="Stryfe"),
            mental_transferal
        ),
    ]

