from . import *

# * Wrecker's Command

def GetAbilities() -> Sequence['Ability']:

    def wreckers_command(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        for villain in Worlds.GetVillains(effect):
            if villain != this.GetBindFace():
                scheme = villain.FindCorrespondingScheme()
                if scheme:
                    this.PlaceThreatOnSchemes([scheme], 1, effect)

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=WRECKER)
        ),
        AbilityFactory.AfterUnitSchemeEnd(
            AbilityType.ForcedResponse,
            CardFinder(name=WRECKER),
            wreckers_command
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCost(Cost("RR"))
    ]

