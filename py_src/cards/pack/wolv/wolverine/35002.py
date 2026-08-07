from . import *

# * Wolverine's Claws

def GetAbilities() -> Sequence['Ability']:

    def wolverines_claws(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.PlayCardsLikeInTurn(effect.targets, effect, ignore_resources_cost=True, give_piercing=True)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wolverines_claws
        ).SetTarget(Event, trait="ATTACK", from_where=["YourHandCards"])
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.TakeDamage(
            lambda effect:
                FacesCounter.GetPrintedCost(effect.targets),
            "YourIdentity"
        ))
    ]

