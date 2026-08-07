from . import *

# * Loki

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ThisCannotTakeDamageWhile(
            conditions=[
                lambda effect, message:
                    Worlds.GetSideSchemes(effect) != []
            ],
        ),
        Loki()
    ]

