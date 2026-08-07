from . import *

# * Taskmaster

def GetAbilities() -> Sequence['Ability']:

    return [
        AfterPlayerChangesToHeroFormDiscardTheEncounterDeckAndTakeDamage(),
    ]

