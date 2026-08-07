from . import *

# Wrist Gauntlets

def GetAbilities() -> Sequence['Ability']:

    def wrist_gauntlets_stun(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)

    def wrist_gauntlets_confuse(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wrist_gauntlets_stun,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "GIANT")
            ],
        ).SetCost(Cost("RR"))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy)
        ,
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wrist_gauntlets_confuse,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "TINY")
            ],
        ).SetCost(Cost("YY"))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Enemy),
    ]

