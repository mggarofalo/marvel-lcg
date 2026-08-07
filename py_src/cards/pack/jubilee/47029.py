from . import *

# Serve and Protect

def GetAbilities() -> Sequence['Ability']:

    def serve_and_protect(effect: 'Effect', message: 'Message.WhenSchemeWouldPlaceThreat') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.PreventThreat("All", effect)

        targets = effect.cost_func.Get(CostFunc.Exhaust).return_exhausted_cards
        Faces.GiveStatus(targets, "Tough", effect)


    return [
        AbilityFactory.WhenThreatWouldBePlacedOn(
            AbilityType.HeroInterrupt,
            MainScheme,
            serve_and_protect,
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust(Select.Alliance(["X-FORCE", "X-MEN"]))),
    ]

