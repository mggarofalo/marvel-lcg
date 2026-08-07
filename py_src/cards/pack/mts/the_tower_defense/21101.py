from . import *

# Focused Defense

def GetAbilities() -> Sequence['Ability']:

    def focused_defense(effect: 'Effect', message: 'Message.AfterPhaseEnd') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        for scheme in Worlds.GetMainSchemes(effect):
            if scheme != this.bind_face:
                this.AttachTo2(scheme, effect)
                break

    return [
        AbilityFactory.TheVillainMatchesAttachedSchemeIsActive(),
        AbilityFactory.AfterPhaseEnd(
            AbilityType.ForcedResponse,
            "Player",
            focused_defense
        ),
    ]

