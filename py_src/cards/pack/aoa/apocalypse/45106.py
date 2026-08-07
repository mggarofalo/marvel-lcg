from . import *

# Cyberpathy

def GetAbilities() -> Sequence['Ability']:

    def cyberpathy(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        schemes = Worlds.GetSideSchemes(effect)
        this.PlaceThreatOnSchemes(schemes, 1, effect)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name="Apocalypse")
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            CardFinder(name="Apocalypse"),
            cyberpathy
        ),
        AbilityFactory.WhenThisBoostAttachTo(
            CardFinder(name="Apocalypse")
        ),
    ]

