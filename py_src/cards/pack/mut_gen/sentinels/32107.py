from . import *

# Targeted for Elimination

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            "YourIdentity",
            without_another_copy=True,
            if_cannot_gain_surge=True
        ),
        AbilityFactory.PlayersCannotChangeForms(
            AbilityType.NonKeyword,
            "AttachedPlayer",
            from_form=Hero,
            to_form=AlterEgo,
            conditions=[
                lambda effect, message:
                    message.trigger.GetControlByPlayer().engaged_minions.FindCardSize(CardFinder2("SENTINEL", Minion)) > 0
            ],
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.Action,
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
    ]

