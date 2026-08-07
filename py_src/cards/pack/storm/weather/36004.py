from . import *

# Thunderstorm

def GetAbilities() -> Sequence['Ability']:

    def thunderstorm(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            thunderstorm
        ).SetTarget(Enemy),
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            "Character",
            attack=1,
        )
    ]

