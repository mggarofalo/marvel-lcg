from . import *

# * Ramming Speed

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            CardFinder(name=BULLDOZER)
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.ForcedInterrupt,
            CardFinder(name=BULLDOZER),
            lambda effect, message:
                message.MustDefendWithAlly(effect),
        ),
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            CardFinder(name=BULLDOZER),
            DiscardThisCard,
        )
    ]

