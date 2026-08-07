from . import *

# * Iron Man: Tony Stark

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.ReduceCostToPlayFaceWhen(
            Upgrade,
            1,
            "AnyPlayer",
            conditions=[
                lambda effect, message:
                    [effect.this] == message.for_targets
            ],
        )
    ]

