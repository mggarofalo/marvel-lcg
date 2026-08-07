from . import *

# Team-Building Exercise

def GetAbilities() -> Sequence['Ability']:

    def team_building_exercise(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.PlayCardsLikeInTurn(effect.targets, effect, update_resources_cost=-1)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            team_building_exercise
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourLikeHandCards", share_trait_with_your_hero=True, canbe_play=True)
    ]

