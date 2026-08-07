from . import *

# Telepathic Restraint

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.YouAreStateWhileThisInPlay(
            lambda effect: effect.this.card.area.GetOwnerPlayer().GetIdentity(),
            "Stunned"
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.Action,
        ).SetCost(Cost("BB")),
    ]

