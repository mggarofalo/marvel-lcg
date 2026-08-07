from . import *

# Homo Superior

def GetAbilities() -> Sequence['Ability']:

    def homo_superior(face: 'CardFace', effect: 'Effect') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        Faces.GiveStatus([face], "Tough", effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Minion,
            if_cannot_gain_surge=True,
            when_attach_operation=homo_superior
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Minion,
            health=5,
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            "Minion",
            ex_operate=lambda face, effect:
                Faces.GiveStatus([face], "Tough", effect)
        ),
    ]

