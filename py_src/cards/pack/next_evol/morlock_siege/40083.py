from . import *

# Pushed to the Limit

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.HeroAction,
        ).SetCostFunc(CostFunc.AttackYou("Attached", card_type=Villain)),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            get_new_value=lambda effect, face, ui:
                len(GetVillainsUnderRouted(effect)) == 1,
            steady=1,
            ex_change_on_event=OnEvent.PlacedCard("Here"),
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Villain,
            get_new_value=lambda effect, face, ui:
                len(GetVillainsUnderRouted(effect)) == 2,
            stalwart=1,
            ex_change_on_event=OnEvent.PlacedCard("Here"),
        ),
    ]

