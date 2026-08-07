from . import *

# * IPAC

def GetAbilities() -> Sequence['Ability']:

    def ipac(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for player in effect.cost_func.Get(CostFunc.DealPlayerEncounterCard).return_players:
            player.DrawUp(2, effect)


    return [
        AbilityFactory.CanPlayThisSupportCard(
        ).SetPlay(only_if_your_identity_has_trait="X-FORCE"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            ipac
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealPlayerEncounterCard(1, "AnyPlayer")),
    ]

