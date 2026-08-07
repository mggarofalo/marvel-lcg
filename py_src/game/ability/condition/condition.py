from core import *
from game.card.face import *
from game.ability import *
from game.message import *
from game.player import *

from game.ability.condition.card_type import ConditionCardType
from game.ability.condition.player_type import ConditionPlayerType
from game.ability.condition.ability_type import ConditionAbilityType
from game.ability.condition.hero_form import ConditionHeroForm
from game.ability.condition.on_event import ConditionOnEventType
from game.ability.condition.once_per import ConditionOncePer

class Condition(ConditionCardType, ConditionPlayerType, ConditionAbilityType, ConditionOnEventType, ConditionHeroForm, ConditionOncePer):

    @staticmethod
    def ThisAttachedTo(effect: 'Effect', to_face: 'CardFace') -> bool:
        from game.card.face.attribute.can_attach import CanAttach
        from game.card.face.card_type import Obligation
        from game.card.face.card_type import StatusCard

        this = effect.this
        # Now, Obligation is also an Asset, see "40031"
        if Obligation.IsType(this):
            return this.GetBindFace() == to_face
        if CanAttach.IsType(this):
            return this.card.area.bind_card != None and \
                this.card.area.bind_card == to_face.card # Check card for "46002"
            # return this.attached_card != None and \
            #     this.attached_card.face == to_face
        if StatusCard.IsType(this):
            if not this.bind_face:
                return False
            # Fix (123640292).json, when villain advanced with a tough states card,
            # damage (e.g. "10031") to the non-advanced villain will ignore tough
            return this.bind_face.card == to_face.card
        return False

    """
    effect.this and face are control by same player
    """
    @staticmethod
    def ThisIsYou(effect: 'Effect', you: 'CardFace|Player|User') -> bool:
        from game.player import Player
        from game.selector import Select
        from game.card.face.card_type import Identity
        from game.card.face.card_type import Upgrade
        from game.card.face.card_type import Event

        identity = None

        if isinstance(you, Player):
            identity = you.GetIdentity()
        elif isinstance(you, User):
            pass
        else:
            if Identity.IsType(you):
                identity = you
            elif Upgrade.IsType(you):
                identity = you.GetBindFace()
            elif Event.IsType(you):
                # identity = identity.GetBindFace()
                pass

        if Identity.IsType(identity):
            assert Player.IsType(identity.GetControlBy())

            return Select.GetYou(effect).GetIdentity() == identity

            # this effect from players' card
            controller = effect.this.GetControlBy()
            if Player.IsType(controller):
                return controller.GetRoleCharacter() == identity and \
                        effect.initiator == identity.GetControlBy() and \
                        effect.initiator == controller
            # this effect from enemies' card
            else:
                return True
        return False

    @staticmethod
    def AgainstYou(effect: 'Effect', message: 'TriggerNonePlayerMessage|Player'):
        from game.player import Player
        # TODO: Test 24007
        # Condition.ThisAttachedTo(effect, message.GetToPlayer().GetIdentity(), True),
        if isinstance(message, Player):
            player = message
        else:
            player = message.to_player

        if effect.IsPlayerInitiator():
            return effect.initiator == player
        else:
            return player != None

    @staticmethod
    def FieldHasNotThisUniqueType(face: 'CardFace', effect: 'Effect') -> bool:
        if not face.IsUnique():
            return True
        else:
            return not effect.world.IsThisUniqueInPlay(face)

    ################################################################################
    # message
    @staticmethod
    def CheckBasicPower(powers: List["CardFace.BASIC_POWER"]|None, message: 'Message.WhenUnitUseBasicPower|Message.AfterUnitUseBasicPower') -> bool:
        from game.card.face.card_type import Ally
        from game.card.face.card_type import Hero
        from game.card.face.card_type import AlterEgo
        from game.card.face.card_type import Minion
        from game.card.face.base import Villain

        if powers == None:
            if Ally.IsType(message.trigger):
                basic_powers = ["ATK", "THW"]
            elif AlterEgo.IsType(message.trigger):
                basic_powers = ["REC"]
            elif Hero.IsType(message.trigger):
                basic_powers = ["ATK", "THW", "DEF"]
            elif Villain.IsType(message.trigger) or Minion.IsType(message.trigger):
                basic_powers = ["ATK", "SCH"]
            else:
                basic_powers = []
        else:
            basic_powers = powers
        for name in basic_powers:
            if name == message.power:
                return True
        return False

