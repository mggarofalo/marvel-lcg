from . import *

# Induced Panic

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
        ),
        AbilityFactory.PlayersCannotTriggerAbility(
            "You",
            "TriggeredAbility",
            on_which_card="YourHero"
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "RandomHandCard",
            card_class="IdentitySpecific",
        ))
    ]

