from . import *

# Temporal Leap

def GetAbilities() -> Sequence['Ability']:

    def temporal_leap(effect: 'Effect', message: 'Message.WhenMainSchemeStageWouldBeCompleted') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.SetBeInstead(effect)

        main_scheme = message.trigger.CastTo(MainScheme)
        side_scheme = effect.cost_func.Get(CostFunc.PutIntoPlay).return_put_cards[0].CastTo(SchemeSide2)
        main_scheme.RemoveThreatFromSchemes([main_scheme], 4, effect)
        this.PlaceThreatOnSchemes([side_scheme], 4, effect)

    return [
        AbilityFactory.WhenMainSchemeStageWouldBeCompleted(
            AbilityType.HeroInterrupt,
            None,
            temporal_leap
        ).SetCostFunc(CostFunc.RemoveFromGame("This"))
        .SetCostFunc(CostFunc.PutIntoPlay(card_type=SchemeSide2, from_where=["VictoryDisplay"]))
    ]

