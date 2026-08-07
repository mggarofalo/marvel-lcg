from . import *

# * Star-Lord

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Ally,
            control_by="You",
            trait="GUARDIAN",
        ),
        AbilityFactory.DoGenerateResources(
            AbilityType.Interrupt,
            "This",
        ).SetCostFunc(CostFunc.DealPlayerEncounterCard(
            1, "Initiator")),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("0", reduce=3),
            None,
            is_play_card=True,
            from_hand=True,
            who_trigger_this_effect="You"
        ).SetName('"What could go wrong?"')
        .LimitOncePerRound(),
    ]

