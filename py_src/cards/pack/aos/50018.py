from . import *

# * The Bellerophon

def GetAbilities() -> Sequence['Ability']:

    def the_bellerophon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        for target in effect.targets:
            target.CastTo(Unit2).DiscardTough(effect, rule="All")

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            the_bellerophon
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'missile'))
        .SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

