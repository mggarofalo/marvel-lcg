from . import *

# * Wasp

def GetAbilities() -> Sequence['Ability']:

    def wasp_giant(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)

    def wasp_tiny(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            wasp_giant,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "GIANT")
            ]
        ).SetTarget(Enemy)
        # .SetDesc('deal 2 damage to an enemy')
        ,
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.HeroResponse,
            "This",
            wasp_tiny,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "TINY")
            ],
        ).SetTarget(Scheme2)
        # .SetDesc('remove 2 threat from a scheme')
    ]

