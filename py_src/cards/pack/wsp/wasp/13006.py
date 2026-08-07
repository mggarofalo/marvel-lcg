from . import *

# Wasp Sting

def GetAbilities() -> Sequence['Ability']:

    def wasp_sting(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 1, effect)

    def wasp_sting_tiny(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wasp_sting,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "GIANT"),
            ]
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy, range=(1, 4), repeat_rules="Health"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            wasp_sting_tiny,
            conditions=[
                lambda effect, message:
                    Condition.YouAreInHeroFrom(effect, "TINY"),
            ]
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

