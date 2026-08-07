from . import *

# * Hela's Crown

def GetAbilities() -> Sequence['Ability']:

    def helas_crown(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        hela = this.GetBindFace().CastTo(Villain)
        Faces.GiveFacedownBoostCards([hela], 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Hela")
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="Hela"),
            helas_crown
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            AttachThisToHela
        ),
    ]

