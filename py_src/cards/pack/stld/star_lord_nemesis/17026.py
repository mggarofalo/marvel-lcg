from . import *

# * Mister Knife

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.FirstCardPlayerRevealsGain(
            Treachery,
            "EngagedPlayer",
            each_phase_round="VillainPhase",
            gain_surge=1
        ),
    ]

