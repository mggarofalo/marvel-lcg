from . import *

# * Bio-Synthetic Wings

def GetAbilities() -> Sequence['Ability']:

    def bio_synthetic_wings(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(1, effect)

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            trait="AERIAL",
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.Interrupt,
            "You",
            bio_synthetic_wings,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "TINY"),
            ]
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

