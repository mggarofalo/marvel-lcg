from . import *

# Stunned

def GetAbilities() -> Sequence['Ability']:

    def trigger_has_this(effect: 'Effect', message: 'Message.WhenUnitWouldAttack|Message.WhenPlayerWouldPlayCard|Message.WhenEffectWouldResolve|Message.WhenEnemyWouldActivate') -> bool:
        this = effect.this.CastTo(StatusCard)
        unit = this.GetBindFace()

        assert not unit.IsStalwart()

        if unit.IsSteady() and unit.stunned < 2:
            return False

        if unit.GetBuff(BuffCannotBeStunned):
            return False

        return True

    def is_basic_attack(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> bool:
        return message.IsBasicAttack() or message.property.attack_in_event

    def set_be_instead(effect: 'Effect', message: 'CanBeInstead'):
        this = effect.this.CastTo(StatusCard)
        message.SetBeInstead(effect)
        unit = this.GetBindFace()
        unit.DiscardStunned(effect, rule="Steady")
        # Hack for "18004"
        if isinstance(message, Message.WhenPlayerWouldPlayCard):
            if message.play_effect.ability.IsLabel('thwart'):
                unit.DiscardConfused(effect, rule="Steady")
        elif isinstance(message, Message.WhenEffectWouldResolve):
            if message.effect.ability.IsLabel('thwart'):
                unit.DiscardStunned(effect, rule="Steady")

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Status,
            "AttachedCharacter",
            set_be_instead,
            conditions=[trigger_has_this, is_basic_attack],
        ),
        AbilityFactory.AfterPlayerTriggerAbility(
            AbilityType.Status,
            "AttachedPlayer",
            None,
            set_be_instead,
            label='attack',
            conditions=[trigger_has_this],
        ),
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.Status,
            "AttachedEnemy",
            set_be_instead,
            power="ATK",
            conditions=[trigger_has_this],
        )
    ]