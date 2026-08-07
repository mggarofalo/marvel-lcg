from typing import TypeAlias
from core import *

from game.ability import *
from game.player import *
from game.card.face import *
from game.message import *

class ConditionPlayerType:

    PLAYER_TYPE: TypeAlias = 'Literal["This", "AttachedPlayer", "You", "EngagedPlayer", "Other"]|PlayerFinder|Player|Literal["AnyPlayer"]'

    # @staticmethod
    # def GetPlayer(check_rule: 'PLAYER_TYPE', from_effect: 'Effect') -> 'Player|None':

    #     if check_rule == None:
    #         return None

    #     if isinstance(check_rule, Player):
    #         return check_rule
    #     if isinstance(check_rule, PlayerFinder):
    #         return ...
    #     if check_rule == "This":
    #         return from_effect.initiator
    #     if check_rule == "AttachedPlayer":
    #         return Condition.ThisAttachedTo(from_effect, identity, True)
    #     if check_rule == "You":
    #         return Condition.ThisIsYou(from_effect, identity)
    #     if check_rule == "EngagedPlayer":
    #         return from_effect.this.CastTo(Minion).GetEngagedPlayer() == check_player
    #     if check_rule == "Other":
    #         return not Condition.ThisIsYou(from_effect, check_player)

    @staticmethod
    def CheckAgainstPlayer(against_player: 'PLAYER_TYPE', message: 'Message.WhenUnitWouldAttack|Message.WhenUnitWouldScheme|Message.AfterUnitSchemeEnd', effect: 'Effect') -> bool:
        if against_player == "AnyPlayer":
            return True
        # For abilities that trigger “after [enemy] attacks you,” “you” refers to the attacked player, even if that player defended with an ally.
        # Attacks are always made against a player. If you choose to defend with an ally, the attack was still against you. It is just defended by your ally. I know that’s not clear with the current rules reference. We’re going to be updating that as soon as our schedule allows.
        if against_player == "You":
            # player = Select.GetYou(effect)
            if effect.context.ask_player:
                return effect.context.ask_player == message.against_player
            else:
                return message.against_player != None
        return ConditionPlayerType.CheckWhichPlayer(against_player, message.against_player, effect)

    @staticmethod
    def CheckWhichPlayerIncludeAllies(check_rule: 'PLAYER_TYPE', message: 'Message.CheckIfUnitCanBeAttackBy|Message.CheckIfSchemeCanBeThwartBy', from_effect: 'Effect') -> bool:
        from game.card.face.card_type import Identity
        from game.card.face.card_type import Upgrade
        # from game.card.face.card_type import Event
        from game.card.face.card_type import Ally

        by_face = message.by_effect.this
        check_player = message.by_effect.initiator

        if check_rule == "You":
            if Ally.IsType(by_face):
                return False
            if Upgrade.IsType(by_face):
                if not Identity.IsType(by_face.GetBindFace()):
                    return False

        # if Event.IsType(by_face):
        #     pass
        return ConditionPlayerType.CheckWhichPlayer(check_rule, check_player, from_effect)

    @staticmethod
    def CheckWhichPlayer(check_rule: 'PLAYER_TYPE', check_player: 'Player|User|None', from_effect: 'Effect') -> bool:
        from game.player import Player
        from game.player.player_finder import PlayerFinder
        from game.card.face.card_type import Minion
        from game.ability.condition import Condition

        if check_rule == "AnyPlayer":
            return True
        if check_player == None:
            return False
        if not isinstance(check_player, Player):
            return False

        identity = check_player.GetIdentity()

        if isinstance(check_rule, Player):
            return check_rule == check_player
        if isinstance(check_rule, PlayerFinder):
            return check_rule.Check(check_player)
        if check_rule == "This":
            return from_effect.initiator == check_player
        if check_rule == "AttachedPlayer":
            return Condition.ThisAttachedTo(from_effect, identity)
        if check_rule == "You":
            return Condition.ThisIsYou(from_effect, identity)
        if check_rule == "EngagedPlayer":
            return from_effect.this.CastTo(Minion).engaged_player == check_player
        if check_rule == "Other":
            return not Condition.ThisIsYou(from_effect, check_player)

