from . import *

# * Nightcrawler: Kurt Wagner

def GetAbilities() -> Sequence['Ability']:

    def nightcrawler(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        message.PreventDamage("All", effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            CardFinder2("X-MEN", Unit2),
            nightcrawler,
            is_from_attack=True,
            who_deal_damage=Enemy,
        ).SetCost(Cost("Y"))
        .SetCostFunc(CostFunc.ReturnToHand("This", to_who="Initiator"))
        .SetTarget("Initiator"),
    ]

