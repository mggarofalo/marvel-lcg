from . import *

# Tactical Scanner

def GetAbilities() -> Sequence['Ability']:

    def tactical_scanner(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            tactical_scanner
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetCostFunc(CostFunc.RemoveFromCampaignLog("This")),
    ]

