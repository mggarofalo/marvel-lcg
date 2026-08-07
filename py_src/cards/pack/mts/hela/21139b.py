from . import *

# * Odin

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.FirstPlayerControlThis(),
        AbilityFactory.ThisCannotHaveEncounterCardsAttached(),
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.WhenCardLeavePlay(
            AbilityType.ForcedInterrupt,
            "This",
            RemoveThisFromGame,
            conditions=[
                lambda effect, message:
                    not message.into_area.flags.is_removed
            ],
        ),
    ]

