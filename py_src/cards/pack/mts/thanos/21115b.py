from . import *

# Balance the Scales


def GetAbilities() -> Sequence['Ability']:
    # The loss sentence is printed on stage 2A, while this is the stage 2B
    # face that can actually complete.  The imported engine text omits it from
    # both faces, so bind the rule explicitly to the completing face.
    return [
        AbilityFactory.IfThisSchemeStageIsCompletedPlayersLoseTheGame(),
    ]
