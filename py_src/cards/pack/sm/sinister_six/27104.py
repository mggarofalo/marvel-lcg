from . import *

# Taunting Presence

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain,
            most_remaining_hp=True,
            if_cannot_operation=ResolveMainSchemeAmbushAndAttach
        ),
        AbilityFactory.ThreatCannotBeRemovedFromWhile(
            CardFinder(name="Light at the End")
            # condition=lambda effect, message:
            #     message.trigger == GetLightAtTheEnd()
        )
    ]

