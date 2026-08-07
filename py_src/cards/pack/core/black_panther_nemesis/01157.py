from . import *

# * Killmonger

def GetAbilities() -> Sequence['Ability']:

    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            conditions=[
                lambda effect, message:
                (
                    # `SpecialAttack`
                    message.would_atk_message != None and \
                    not message.would_atk_message.IsBasicAttack() and \
                    Upgrade.IsType(message.would_atk_message.by_effect.this) and \
                    message.would_atk_message.by_effect.this.HasTrait('BLACK PANTHER')
                ) or (
                    # `MoveDamage`
                    message.by_effect.this.HasTrait('BLACK PANTHER')
                ),
            ]
        )
    ]

