from typing import Final
from core import *
from game.card import *
from game.card.face import *
from game.deck import *
from game.ability import *
from game.message import *
from game.world import *
from game.player import *
from game.world.game_area import *

from engine.lib import TransText

"""
Name rule
_Text           Only for show text (UI) or debug info
_Internal       Do not catch this message

When ... Would  You can modify some property by handle event like this
                Or instead this event
                Always `CanBeInstead`

When            This even can be safely handle by the card effect

After

`group`         This param only appear in Card event, and only effect the UI
"""

from game.message.message_type import TextMessage
from game.message.message_type import TriggerPlayerMessage
from game.message.message_type import TriggerFaceMessage
from game.message.message_type import TriggerUnitMessage
from game.message.message_type import TriggerSchemeMessage
from game.message.message_type import CalculateMessage
from game.message.message_type import CheckIfMessage
from game.message.message_type import GettingMessage
from game.message.message_type import CanBeInstead

Unused(TextMessage, TriggerPlayerMessage, TriggerFaceMessage, TriggerUnitMessage, TriggerSchemeMessage, CalculateMessage, CheckIfMessage, CanBeInstead, GettingMessage)

from game.message.sender.sender_player import SenderPlayer
from game.message.sender.sender_round import SenderRound
from game.message.sender.sender_card import SenderCard
from game.message.sender.sender_token import SenderTokenCounter
from game.message.sender.sender_deck import SenderDeck
from game.message.sender.sender_scheme import SenderScheme
from game.message.sender.sender_enemy import SenderEnemy
from game.message.sender.sender_unit import SenderUnit
from game.message.sender.sender_friend import SenderFriend
from game.message.sender.sender_damage import SenderDamage
from game.message.sender.sender_effect import SenderEffect
from game.message.sender.sender_environment import SenderEnvironment

CATEGORY_NAME = "SENDER"

# Message Factory
class Message(SenderRound, SenderPlayer, SenderCard, SenderTokenCounter, SenderDeck, SenderScheme, SenderEnemy, SenderUnit, SenderFriend, SenderDamage, SenderEffect, SenderEnvironment):

    class TextLog(TextMessage):
        def __init__(self, text: str) -> None:
            from engine.log import Log
            from game.test import Test
            if not Test.IsInTesting():
                Log.PrintGameInfo(text, sound_name="")

    class TextRender(TextMessage):
        def __init__(self, origin_text: str, world: 'World') -> None:
            from game.test import Test
            super().__init__(world=world)

            if not Test.IsInTesting():
                text = TransText(origin_text)
                self.Present(text, "")
            pass

    ################################################################################
    #
    class WhenMessageBeInstead(Message2):
        def __init__(self, message: 'Message2', by_effect: 'Effect') -> None:
            self.message: Final = message
            self.by_effect: Final = by_effect
            faces = [by_effect.this]
            if isinstance(message, TriggerMessage):
                faces.append(message.trigger)

            super().__init__(world=by_effect.world)

            text = TransText("{message} is instead by {by_effect}", message=message.GetDisplayName(), by_effect=by_effect.this)
            self.Present(text, "activate", *faces)

    ################################################################################
    # Check
    class CheckIfUnitCanBeAttackBy(CheckIfMessage):
        def __init__(self, attacked: 'Unit2', effect: 'Effect') -> None:
            from game.card.face.base import Unit2
            from game.card.face.card_type import Event
            from game.card.face.card_type import Support
            from game.card.face.card_type import Upgrade
            
            self.can_be_attack = True

            this = effect.this
            attacker = this
            if Unit2.IsType(this):
                attacker = this
            elif Event.IsType(this):
                attacker = this.GetControlByOrOwner().GetRoleCharacter()
            elif Upgrade.IsType(this):
                attacker = this.GetBindFace()
            elif Support.IsType(this):
                attacker = this
            else:
                attacker = this

            self.attacker: Final = attacker
            self.being_attack: Final = attacked # who is being attack
            self.by_effect: Final = effect
            # effect: by effect
            super().__init__(effect.this)
            self.AddRelatedFace(attacker, attacked, effect)

        def SetCannotBeAttack(self, by_effect: 'Effect'):
            self.can_be_attack = False
            self.SetCauseBy(by_effect)

    class CheckIfSchemeCanBeThwartBy(CheckIfMessage):
        def __init__(self, scheme: 'Scheme2', effect: 'Effect') -> None:
            from game.card.face.base import Unit2
            from game.card.face.card_type import Event
            from game.card.face.card_type import Support
            from game.card.face.card_type import Upgrade
            
            self.can_be_thwart: bool = True

            this = effect.this
            who_thwart = this
            if Unit2.IsType(this):
                who_thwart = this
            elif Event.IsType(this):
                who_thwart = this.GetControlByOrOwner().GetRoleCharacter()
            elif Upgrade.IsType(this):
                who_thwart = this.GetBindFace()
            elif Support.IsType(this):
                who_thwart = this
            else:
                who_thwart = this
            self.who_thwart = who_thwart

            self.scheme: Final = scheme
            self.by_effect: Final = effect
            super().__init__(effect.this)
            self.AddRelatedFace(scheme, who_thwart, effect)

        def SetCannotBeThwart(self, by_effect: 'Effect'):
            self.can_be_thwart = False
            self.SetCauseBy(by_effect)

    class CheckIfUnitCanDefendAgainstAttack(CheckIfMessage):
        def __init__(self, unit: 'Ally|Hero', attacker: 'Unit2') -> None:
            self.can_defend: bool = True
            self.friend = unit
            self.attacker: Final = attacker
            self.check_unit: Final = unit
            super().__init__(unit)
            self.AddRelatedFace(attacker, unit)

        def SetCannotDefend(self, by_effect: 'Effect'):
            self.can_defend = False
            self.SetCauseBy(by_effect)

    class CheckIfUnitCanAttack(CheckIfMessage):
        def __init__(self, unit: 'Unit2', effect: 'Effect') -> None:
            self.can_attack = True
            self.by_effect: Final = effect
            self.check_unit: Final = unit
            super().__init__(unit)

        def SetCannotAttack(self, by_effect: 'Effect'):
            self.can_attack = False
            self.SetCauseBy(by_effect)

    class CheckIfUnitCanThwart(CheckIfMessage):
        def __init__(self, unit: 'Unit2', effect: 'Effect') -> None:
            self.can_thwart = True
            self.by_effect = effect
            self.check_unit: Final = unit
            super().__init__(unit)

        def SetCannotThwart(self, by_effect: 'Effect'):
            self.can_thwart = False
            self.SetCauseBy(by_effect)

    class CheckIfUnitCanRecover(CheckIfMessage):
        def __init__(self, unit: 'AlterEgo', effect: 'Effect') -> None:
            self.can_recover = True
            self.by_effect = effect
            self.check_identity: Final = unit
            super().__init__(unit)

        def SetCannotRecover(self, by_effect: 'Effect'):
            self.can_recover = False
            self.SetCauseBy(by_effect)

    class CheckIfFaceCanAttachTo(CheckIfMessage):
        def __init__(self, which_face: 'CardFace', to_face: 'CardFace', effect: 'Effect') -> None:
            self.can_attach_to: bool = True
            self.to_face: Final = to_face
            self.upgrade: Final = which_face
            super().__init__(effect.this)

        def SetCannotAttachTo(self, by_effect: 'Effect'):
            self.can_attach_to = False
            self.SetCauseBy(by_effect)

    class CheckIfFaceApplyUniqueRule(CheckIfMessage):
        def __init__(self, face: 'CardFace') -> None:
            self.which_face: Final = face
            self.apply_unique_rule = True
            super().__init__(face)

        def IgnoreUniqueRule(self, by_effect: 'Effect'):
            self.apply_unique_rule = False

    class CheckIfFaceIsLikeInHand(CheckIfMessage):
        def __init__(self, face: 'CardFace') -> None:
            self.which_face: Final = face
            self.is_like_in_hand = False
            super().__init__(face)
            self.AddRelatedFace(face)

        def SetIsLikeInHand(self, by_effect: 'Effect'):
            self.is_like_in_hand = True

    class CheckIfFaceCanBeReadied(CheckIfMessage):
        def __init__(self, face: 'CardFace', by_effect: 'Effect') -> None:
            self.which_face: Final = face
            self.is_can_be_readied = True
            self.by_effect: Final = by_effect
            super().__init__(face)
            self.AddRelatedFace(face)

        def SetCannotBeReadied(self, by_effect: 'Effect'):
            self.is_can_be_readied = False

    class CheckIfRefersToTheVillainRefersToAttached(CheckIfMessage):
        def __init__(self, face: 'CardFace') -> None:
            self.which_face: Final = face
            self.refers_to = False
            super().__init__(face)
            self.AddRelatedFace(face)

        def SetRefersTo(self, by_effect: 'Effect'):
            self.refers_to = True

    ################################################################################
    # Get
    ################################################################################
    class GettingMainScheme(GettingMessage):
        def __init__(self, game_area: 'GameArea', face: 'CardFace') -> None:
            self.game_area: Final = game_area
            self.by_face: Final = face
            super().__init__(world=face.card.world)

        def SetReturn(self, val: 'MainScheme'):
            self.return_value = val

    class GettingScenario(GettingMessage):
        def __init__(self, game_area: 'GameArea|None', world: 'World') -> None:
            self.game_area: Final = game_area
            super().__init__(world=world)

        def SetReturn(self, val: 'Scenario'):
            self.return_value = val

    class GettingVillain(GettingMessage):
        def __init__(self, game_area: 'GameArea|None', world: 'World') -> None:
            self.game_area: Final = game_area
            super().__init__(world=world)

        def SetReturn(self, val: 'Villain'):
            self.return_value = val

    class GettingUnitAdditionalTough(TriggerUnitMessage, GettingMessage):
        def __init__(self, unit: 'Unit2') -> None:
            super().__init__(trigger=unit)

        def SetReturn(self, val: int):
            self.return_value = val

    # class CheckIfUnitCannotBeStunned(CheckIfMessage):
    #     def __init__(self, unit: 'Unit2') -> None:
    #         self.canbe_stunned = True
    #         self.check_unit: Final = unit
    #         super().__init__(unit)

    #     def SetCannotBeStunned(self):
    #         self.canbe_stunned = False

    # class CheckIfUnitCannotBeConfused(CheckIfMessage):
    #     def __init__(self, unit: 'Unit2') -> None:
    #         self.canbe_confused = True
    #         self.check_unit: Final = unit
    #         super().__init__(unit)

    #     def SetCannotBeConfused(self):
    #         self.canbe_confused = False

    class CheckIfEffectIsIgnoreKeyWord(CheckIfMessage):
        def __init__(self, effect: 'Effect', keyword: Literal['Guard', 'Patrol', 'Crisis']) -> None:
            self.effect: Final = effect
            self.keyword: Final = keyword
            self.is_ignore_this_keyword: bool=False
            super().__init__(effect.this)
            self.AddRelatedFace(effect)

        def SetIgnore(self):
            self.is_ignore_this_keyword = True

    ################################################################################
    # Check
    ################################################################################
    # class WhenCheckPlayCardCost(ToPlayerMessage):
    #     def __init__(self, player: 'Player', effect: 'Effect', paid_res: 'Resources') -> None:
    #         self.face: Final = effect
    #         self.for_effect = effect
    #         self.paid_res = paid_res
    #         super().__init__(player=player)

    #     def UpdateNeedCost(self, diff: int, by_effect: 'Effect'):
    #         assert self.for_effect.this_effect_cost
    #         self.for_effect.this_effect_cost += diff


    ################################################################################
    # script definition event
    class WhenScenarioEvent(Message2):
        def __init__(self, text: str, *params: Any, world: 'World') -> None:
            self.text = text
            self.params = params
            super().__init__(world=world)

    class CheckIfAttackMessageHasKeyword(CheckIfMessage, AttackerMessage):
        def __init__(self, check_attack: 'Message.WhenUnitWouldAttackUnit', by_effect: 'Effect') -> None:
            self.check_attack: Final = check_attack
            self.property: Final = check_attack.property
            self.by_effect: Final = by_effect
            super().__init__(attacker=check_attack.attacker, attacked=check_attack.attacked_targets, would_atk_message=check_attack.would_atk_message, by_face=by_effect.this)
            self.AddRelatedFace(self.attacker, by_effect)

        # An attack with the ranged keyword ignores the retaliate keyword.
        def GainRanged(self, by_effect: 'Effect'):
            if not self.property.ranged:
                from game.message import Message
                self.property.ranged = True
                message = Message.WhenAttackGainKeyword('ranged', by_effect, self)
                message.Send()
        def GainOverKill(self, by_effect: 'Effect'):
            if not self.property.overkill:
                from game.message import Message
                self.property.overkill = True
                message = Message.WhenAttackGainKeyword('overkill', by_effect, self)
                message.Send()
        def GainPiercing(self, by_effect: 'Effect'):
            if not self.property.piercing:
                from game.message import Message
                self.property.piercing = True # No tough
                message = Message.WhenAttackGainKeyword('piercing', by_effect, self)
                message.Send()
        def SetDealIndirectDamage(self, by_effect: 'Effect'):
            if not self.property.deal_indirect_damage:
                self.property.deal_indirect_damage = True
        def SetIgnoreRetaliate(self, by_effect: 'Effect'):
            if not self.property.ignore_retaliate:
                self.property.ignore_retaliate = True
        def SetLostPiercing(self, by_effect: 'Effect'):
            if not self.property.lost_piercing:
                self.property.lost_piercing = True

        def IsBasicAttack(self) -> bool:
            return self.check_attack.would_atk_message.IsBasicAttack()

    ################################################################################
    #
    class AfterCardsGenerated_Text(TextMessage):
        def __init__(self, card: 'Card') -> None:
            super().__init__(world=card.world)

            faces = [card.face]
            text = TransText("{faces} created", faces=faces)
            self.Present(text, "set", *faces)

