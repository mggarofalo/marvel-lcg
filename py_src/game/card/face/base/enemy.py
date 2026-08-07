from core import *
from game.card.face.base import *
from game.card.face import *
from game.ability import *
from game.ability.factory import *
from game.message import *
from game.player import *
from cards.paper import Paper

class Enemy(HasScheme, CanScheme, HasAttack, CanAttack, Unit2, CanBoost, CanPlaceCounter):
    @override
    def __init__(self, paper: 'Paper') -> None:
        super().__init__(paper)

    ################################################################################
    #
    def DoSchemes(self, against_player: 'Player', by_effect: 'Effect', *,
                    property: 'SchemeProperty|None'=None,
                    operation: Callable[['Message.WhenUnitWouldScheme'], Any]|None=None,
                    for_each_threat_placed: Callable[[int], None|Any]|None=None,
                    ) -> 'Message.AfterUnitSchemeEnd|None':
        from game.operate.worlds import Worlds
        from game.operate.effects import Effects
        from game.operate.run_at import RunAt
        from game.effect.rule import GameRule
        from game.message.message_type import InActivationMessage
        scheme = Worlds.FindMainScheme(self)
        if not scheme:
            return None

        bind_message = by_effect.bind_message 
        if isinstance(bind_message, InActivationMessage) and \
            bind_message.activate_message and \
            not bind_message.activate_message.is_be_instead:
            RunAt.AfterEnemyActivationEnd(
                by_effect,
                bind_message.activate_message,
                lambda:
                    self.DoSchemes(
                        against_player,
                        GameRule(by_effect.this),
                        property=property,
                        operation=operation,
                    )
            )
            return

        action_effects: List['Effect'] = []
        if operation:
            action_effects += by_effect.this.effect.RegisterTemp(
                AbilityFactory.WhenUnitWouldScheme(
                    AbilityType.Temp0,
                    None,
                    lambda effect, message:
                        operation(message),
                    by_effect=by_effect,
                ),
                unregister_after_exec=False
            )

        if property == None:
            property = SchemeProperty(is_basic_power=True, against_player=against_player)
        else:
            property.against_player = against_player

        return_value = self.BasicSchemes(by_effect, property=property)
        Effects.UnRegister(action_effects)

        if for_each_threat_placed and return_value and return_value.placed_threat > 0:
            for_each_threat_placed(return_value.placed_threat)

        return return_value

    def DoAttackYou(self, against_player: 'Player|Literal["YourLeader"]', by_effect: 'Effect', *,
                    property: 'AttackProperty|None'=None,
                    operation: Callable[['Message.WhenUnitWouldAttack'], Any]|None=None,
                    operation_end: Callable[['Message.AfterUnitAttackEnd'], Any]|None=None,
                    if_no_character_takes_damage: Callable[[], None]|None=None,
                    if_characters_damaged_by_this: Callable[[List['Unit2']], None|int]|None=None,
                    if_this_attack_deals_damage: Callable[[int], None|Any]|None=None,
                    if_no_attack_was_made: Callable[[], None]|None=None,
                    if_unit_defends_against: Callable[['Unit2', int], None]|None=None,
                    if_undefended: Callable[[], None]|None=None,
                    you_cannot_play_event: bool=False,
                    other_characters_cannot_defend: bool=False,
                    ) -> 'Message.AfterUnitAttackEnd|None':
        from game.card.face.card_type import Event
        from game.operate.effects import Effects
        from game.operate.run_at import RunAt
        from game.effect.rule import GameRule
        from game.message.message_type import InActivationMessage

        if against_player == "YourLeader":
            if if_no_character_takes_damage:
                if_no_character_takes_damage()
            if if_no_attack_was_made:
                if_no_attack_was_made()
            return None

        bind_message = by_effect.bind_message 
        if isinstance(bind_message, InActivationMessage) and \
            bind_message.activate_message and \
            not bind_message.activate_message.is_be_instead:
            RunAt.AfterEnemyActivationEnd(
                by_effect,
                bind_message.activate_message,
                lambda:
                    self.DoAttackYou(
                        against_player,
                        GameRule(by_effect.this),
                        property=property,
                        operation=operation,
                        operation_end=operation_end,
                        if_no_character_takes_damage=if_no_character_takes_damage,
                        if_characters_damaged_by_this=if_characters_damaged_by_this,
                        if_this_attack_deals_damage=if_this_attack_deals_damage,
                        if_no_attack_was_made=if_no_attack_was_made,
                        if_unit_defends_against=if_unit_defends_against,
                        if_undefended=if_undefended,
                        you_cannot_play_event=you_cannot_play_event,
                        other_characters_cannot_defend=other_characters_cannot_defend,
                    )
            )
            return

        action_effects: List['Effect'] = []
        if operation:
            action_effects += by_effect.this.effect.RegisterTemp(
                AbilityFactory.WhenUnitWouldAttack(
                    AbilityType.Temp0,
                    self,
                    lambda effect, activate_message:
                        operation(activate_message),
                    by_effect=by_effect,
                ),
                unregister_after_exec=False
            )
        if you_cannot_play_event:
            action_effects += by_effect.this.effect.RegisterTemp(
                AbilityFactory.PlayersCannotPlayCardWhile(
                    against_player,
                    Event,
                ).NoOutOfPlayLimit(),
                unregister_after_exec=False
            )

        identity = against_player.GetIdentity()
        if other_characters_cannot_defend:
            action_effects += by_effect.this.effect.RegisterTemp(
                *AbilityFactory.UnitCannotDefend(
                    CardFinder(non_face=identity),
                    self,
                    cannot_trigger_defense_ability=True,
                ),
                unregister_after_exec=False
            )

        if property == None:
            property = AttackProperty(is_basic_power=True, against_player=against_player)
        else:
            property.against_player = against_player

        return_val = self.BasicAttack([identity], by_effect,
                                        property=property,
                                        if_no_character_takes_damage=if_no_character_takes_damage,
                                        if_characters_damaged_by_this=if_characters_damaged_by_this,
                                        for_each_damage_dealt_by_this=if_this_attack_deals_damage,
                                        if_no_attack_was_made=if_no_attack_was_made,
                                        if_unit_defends_against=if_unit_defends_against,
                                        if_undefended=if_undefended,
                                        )

        if operation_end and return_val:
            operation_end(return_val)

        Effects.UnRegister(action_effects)
        return return_val

    def DoActivate(self, against_player: 'Player', by_effect: 'Effect', operation: Callable[['Message.WhenEnemyActivateAgainstYou'], Any]|None=None, do_not_give_boost: bool=False, if_no_activate: Callable[[], Any]|None=None) -> 'Message.AfterUnitAttackEnd|Message.AfterUnitSchemeEnd|None':
        from game.operate.effects import Effects

        if operation:
            action_effects = by_effect.this.effect.Registers(
                AbilityFactory.WhenEnemyActivateAgainstYou(
                    AbilityType.Temp0,
                    None,
                    lambda activate_effect, message:
                        operation(message),
                    conditions=[
                        lambda activate_effect, activate_message:
                            self == activate_message.trigger and \
                            by_effect == activate_message.by_effect
                    ],
                )
            )
        else:
            action_effects = None

        would_act_message = Message.WhenEnemyWouldActivate(self, against_player)
        would_act_message.Send()
        if would_act_message.is_be_instead:
            return None

        if against_player.IsAlterEgo():
            message = self.DoSchemes(against_player, by_effect, property=SchemeProperty(against_player=against_player, do_not_give_boost=do_not_give_boost, would_act_message=would_act_message))
        else:
            message = self.DoAttackYou(against_player, by_effect, property=AttackProperty(against_player=against_player, do_not_give_boost=do_not_give_boost, would_act_message=would_act_message))

        Effects.UnRegister(action_effects)

        if not message:
            if if_no_activate:
                if_no_activate()

        return message
