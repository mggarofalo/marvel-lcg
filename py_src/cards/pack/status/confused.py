from . import *

# Confused

def GetAbilities() -> Sequence['Ability']:

    def trigger_has_this(effect: 'Effect', message: 'Message.WhenUnitWouldScheme|Message.WhenUnitWouldThwart|Message.WhenPlayerWouldPlayCard|Message.WhenEffectWouldResolve|Message.WhenEnemyWouldActivate') -> bool:
        if isinstance(message, Message.WhenUnitWouldThwart) and \
            not message.by_effect.ability.is_like_thwart:
            return False

        this = effect.this.CastTo(StatusCard)
        unit = this.GetBindFace()

        assert not unit.IsStalwart()

        if unit.IsSteady() and unit.confused < 2:
            return False

        if unit.GetBuff(BuffCannotBeConfused):
            return False

        return True

    def set_be_instead(effect: 'Effect', message: 'CanBeInstead'):
        this = effect.this.CastTo(StatusCard)
        # for "18004", though "Stunned" pass the condition, but this set be instead will make it not continue broadcast, so the operation of "Stunned" will not execute
        message.SetBeInstead(effect)
        unit = this.GetBindFace()
        unit.DiscardConfused(effect, rule="Steady")
        # Hack for "18004"
        if isinstance(message, Message.WhenPlayerWouldPlayCard):
            if message.play_effect.ability.IsLabel('attack'):
                unit.DiscardStunned(effect, rule="Steady")
        elif isinstance(message, Message.WhenEffectWouldResolve):
            if message.effect.ability.IsLabel('attack'):
                unit.DiscardStunned(effect, rule="Steady")

    return [
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.Status,
            "AttachedCharacter",
            set_be_instead,
            conditions=[trigger_has_this]
        ),
        AbilityFactory.WhenUnitWouldThwart(
            AbilityType.Status,
            "AttachedCharacter",
            set_be_instead,
            is_basic_thwart=True,
            conditions=[trigger_has_this]
        ),
        AbilityFactory.AfterPlayerTriggerAbility(
            AbilityType.Status,
            "AttachedPlayer",
            None,
            set_be_instead,
            label='thwart',
            conditions=[trigger_has_this],
        ),
        AbilityFactory.WhenEnemyWouldActivate(
            AbilityType.Status,
            "AttachedEnemy",
            set_be_instead,
            power="SCH",
            conditions=[trigger_has_this],
        )
    ]

