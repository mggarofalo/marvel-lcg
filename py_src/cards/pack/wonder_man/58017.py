from . import *

# Bombs Away

def GetAbilities() -> Sequence['Ability']:

    def bombs_away(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            bombs_away
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust("YouControlUnit", trait="AERIAL"))
        .SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

