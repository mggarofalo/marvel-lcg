from . import *

# * Odin

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.GiveAbilityToInPlayWhenApplyThis(
            "This",
            abilities=[
                AbilityFactory.FirstPlayerControlThis(),
                AbilityFactory.ThisCannotHaveCardsAttached(),
                *AbilityFactory.ThisNotCountAllyLimit(),
            ]
        ),
        AbilityFactory.WhenCardLeavePlay(
            AbilityType.NonKeyword,
            "This",
            lambda effect, message:
                Worlds.SetGameOver(False, effect)
        )
    ]

