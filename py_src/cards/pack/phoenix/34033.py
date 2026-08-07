from . import *

# Psychic Misdirection

def GetAbilities() -> Sequence['Ability']:

    def psychic_misdirection(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        target = effect.targets[0]

        def action(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage'):
            message.ChangeDealtToTarget(target, effect)

        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldTakeDamage(
                AbilityType.Temp0,
                None,
                action,
                is_from_attack=message
            ),
            unregister_after_exec=True,
            until_event_end=message
        )

    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.HeroInterrupt,
            Enemy,
            psychic_misdirection,
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC").SetLabel('defense')
        .SetTarget(Enemy, exclude="Trigger"),
    ]

