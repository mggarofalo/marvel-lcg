from . import *

# Explosive Arrow

def GetAbilities() -> Sequence['Ability']:

    def explosive(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            explosive,
        ).SetPlay()
        .SetTarget("VillainAndEngagedSamePlayerMinion")
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            name=HAWKEYE_BOW_NAME,
            from_where=["YouControlCards"])
        )
    ]
