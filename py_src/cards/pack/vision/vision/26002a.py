from . import *

# Intangible

def GetAbilities() -> Sequence['Ability']:

    def intangible(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.ReduceDamage(2, effect)


    return [
        *AbilityFactory.UnitCannotAttackTarget(
            CardFinder(name="Vision"),
            cannot_attack=True,
            cannot_trigger_attack_ability=True
        ),
        *AbilityFactory.UnitCannotDefend(
            CardFinder(name="Vision"),
            None,
            cannot_trigger_defense_ability=True
        ),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.NonKeyword,
            CardFinder(name="Vision"),
            intangible,
            is_from_attack=True
        ),
    ]

