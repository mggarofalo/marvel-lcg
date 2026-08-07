from . import *

# Hard to Ignore

def GetAbilities() -> Sequence['Ability']:

    def hard_to_ignore(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "YourHero",
            hard_to_ignore,
            take_no_damage=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(MainScheme),
    ]

