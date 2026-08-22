from . import *

# * Iron Man: Tony Stark

def GetAbilities() -> Sequence['Ability']:

    return [
        # "Reduce the cost to play each upgrade **on Iron Man** by 1." The
        # reduction is target-dependent, so the checker has to price each
        # legal target of the upgrade separately -- `when_this_is_the_target`
        # is what tells it to (MARVEL-140).
        AbilityFactory.ReduceCostToPlayFaceWhen(
            Upgrade,
            1,
            "AnyPlayer",
            when_this_is_the_target=True,
        )
    ]
