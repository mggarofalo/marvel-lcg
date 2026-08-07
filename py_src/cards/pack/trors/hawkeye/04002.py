from . import *

# * Hawkeye's Bow

def GetAbilities() -> Sequence['Ability']:

    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        ),
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.NonKeyword,
            "YourHero",
            lambda effect, message:
                message.GainRanged(effect),
            is_basic_attack=False,
            conditions=[
                lambda effect, message:
                    message.by_effect.this.HasTrait('ARROW') and \
                    message.by_effect.ability.is_like_attack,
            ]
        )
    ]
