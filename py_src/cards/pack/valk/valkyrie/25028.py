from . import *

# Trouble in Otherworld

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.UnitCannotAttackTarget(
            CardFinder(name="Valkyrie"),
            cannot_attack=HAS_DEATH_GLOW_FINDER,
        ),
        AbilityFactory.PlayerActionToRemoveThisFromGame(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("YB"))
    ]

