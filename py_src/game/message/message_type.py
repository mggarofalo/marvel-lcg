from typing import Final
from core import *
from game.card.face import *
from game.deck import *
from game.effect import *
from game.message import *
from game.world import *
from game.player import *

class WorldKwargs(TypedDict):
    world: 'World'

################################################################################
#
class NoSendMessage(Message2):
    @override
    def Send(self):
        pass

class LikeFakeMessage(Message2):
    pass

class TextMessage(NoSendMessage, Message2):
    def __init__(self, **kwargs: Unpack[WorldKwargs]) -> None:
        super().__init__(send_resolve_message=False, **kwargs)

class NoSendResolve(Message2):
    def __init__(self, **kwargs: Unpack[WorldKwargs]) -> None:
        super().__init__(send_resolve_message=False, **kwargs)

################################################################################
#
class TriggerNonePlayerMessage(Message2):
    def __init__(self, player: 'Player|None', **kwargs: Unpack[WorldKwargs]) -> None:
        self.to_player: Final = player
        super().__init__(**kwargs)

    def GetToPlayer(self) -> 'Player':
        from game.player import Player
        assert type(self.to_player) is Player
        return self.to_player

    def IsToPlayer(self) -> bool:
        from game.player import Player
        return type(self.to_player) is Player

class TriggerPlayerMessage(TriggerNonePlayerMessage):
    def __init__(self, player: 'Player', **kwargs: Any) -> None:
        kwargs.pop('world', None)  # Safely remove 'world' if it exists
        super().__init__(player=player, world=player.world, **kwargs)

################################################################################
#
class TriggerMessage(Message2):
    def __init__(self, trigger: 'TC', type: Type[TC], **kwargs: Any) -> None:
        self.private_trigger: Final = trigger
        kwargs.pop('world', None)  # Safely remove 'world' if it exists
        super().__init__(**kwargs, world=self.private_trigger.card.world)
        self.AddRelatedFace(trigger)

    @property
    def trigger(self) -> 'CardFace':
        return self.private_trigger

class TriggerFaceMessage(TriggerMessage):
    def __init__(self, trigger: 'CardFace', **kwargs: Any) -> None:
        from game.card.face.card_face import CardFace
        super().__init__(trigger, CardFace, **kwargs)

class TriggerUnitMessage(TriggerMessage):
    def __init__(self, trigger: 'Unit2', **kwargs: Any) -> None:
        from game.card.face.base import Unit2
        super().__init__(trigger, Unit2, **kwargs)

    @property
    def trigger(self) -> 'Unit2':
        from game.card.face.base import Unit2
        assert Unit2.IsType(self.private_trigger)
        return self.private_trigger

class TriggerSchemeMessage(TriggerMessage):
    def __init__(self, trigger: 'Scheme2', **kwargs: Any) -> None:
        from game.card.face.base import Scheme2
        super().__init__(trigger, Scheme2, **kwargs)

    @property
    def trigger(self) -> 'Scheme2':
        from game.card.face.base import Scheme2
        assert Scheme2.IsType(self.private_trigger)
        # Cannot use `CastTo` because of "32088a"
        return self.private_trigger

################################################################################
#
class TargetsMessage(Message2):
    def __init__(self, targets: Sequence['CardFace'], **kwargs: Any) -> None:
        self.targets_internal = list(targets)
        super().__init__(**kwargs)
        self.AddRelatedFace(*targets)

    @property
    def targets(self) -> Sequence['CardFace']:
        return self.targets_internal

    def AddTarget(self, *face: 'CardFace'):
        self.targets_internal += list(face)

    def ReplaceTarget(self, *face: 'CardFace'):
        self.targets_internal = list(face)

class AttackMessage(Message2):
    def __init__(self, would_atk_message: 'Message.WhenUnitWouldAttack|None|List[Message.WhenUnitWouldAttack]', **kwargs: Any) -> None:
        if would_atk_message == None:
            would_atk_messages = []
        elif isinstance(would_atk_message, list):
            would_atk_messages = would_atk_message
        else:
            would_atk_messages = [would_atk_message]
        self.would_atk_messages = would_atk_messages
        super().__init__(**kwargs)

    @property
    def has_resolves_defense_labeled_ability(self):
        for would_atk_message in self.would_atk_messages:
            if would_atk_message.has_resolves_defense_labeled_ability_internal:
                return would_atk_message.has_resolves_defense_labeled_ability_internal
        return None

class AttackerMessageInternal(TargetsMessage, AttackMessage):
    def __init__(self, attacker: 'Unit2|None', attacked: List['Unit2'], **kwargs: Any) -> None:
        self.attacker_internal: Final = attacker
        super().__init__(targets=attacked, **kwargs)
        self.AddRelatedFace(self.attacker)

    @property
    def attacker(self) -> 'Unit2|None':
        return self.attacker_internal

    @property
    def attacked_targets(self) -> Sequence['Unit2']:
        from game.operate.filter import Filter
        from game.card.face.base import Unit2
        return Filter.ByType(self.targets, Unit2)

class AttackerNoneMessage(AttackerMessageInternal):
    def __init__(self, being_atk_message: 'Message.WhenUnitBeingAttack|None', attacker: 'Unit2|None'=None, **kwargs: Any) -> None:
        if not attacker:
            attacker = being_atk_message.attacker if being_atk_message else None
        # self.attacked: Final = being_atk_message.attacked if being_atk_message else []
        self.being_atk_message: Final = being_atk_message

        super().__init__(attacker=attacker, attacked=[being_atk_message.trigger] if being_atk_message else [], **kwargs, would_atk_message=being_atk_message.would_atk_message if being_atk_message else None)

class AttackerNoneOldMessage(AttackerMessageInternal):
    def __init__(self, attacker: 'Unit2|None', would_atk_message: 'Message.WhenUnitWouldAttack|None', **kwargs: Any) -> None:
        super().__init__(attacker=attacker, would_atk_message=would_atk_message, **kwargs)

class AttackerMessage(AttackerMessageInternal):
    def __init__(self, attacker: 'Unit2', attacked: List['Unit2'], would_atk_message: 'Message.WhenUnitWouldAttack|None', **kwargs: Any) -> None:
        kwargs.pop('world', None)
        super().__init__(attacker=attacker, attacked=attacked, world=attacker.card.world, would_atk_message=would_atk_message, **kwargs)

    @property
    def attacker(self) -> 'Unit2':
        assert self.attacker_internal
        return self.attacker_internal

class SchemerMessage(Message2):
    def __init__(self, schemer: 'Unit2', **kwargs: Any) -> None:
        self.schemer: Final = schemer
        super().__init__(**kwargs)

    # @property
    # def schemed_targets(self) -> List['Scheme2']:
    #     from game.operate.faces import Faces
    #     from game.card.face.base import Scheme2
    #     return Filter.FilterByType(self.targets, Scheme2)

class ThwarterMessage(TargetsMessage):
    def __init__(self, thwarter: 'Unit2', thwarted: List['Scheme2'], **kwargs: Any) -> None:
        self.thwarter: Final = thwarter
        super().__init__(targets=thwarted, **kwargs)

    @property
    def thwarted_targets(self) -> List['Scheme2']:
        from game.operate.filter import Filter
        from game.card.face.base import Scheme2
        return Filter.ByType(self.targets, Scheme2)

class DefenderNoneMessage(Message2):
    def __init__(self, defender: 'Unit2|None', **kwargs: Any) -> None:
        self.defender_internal: Final = defender
        super().__init__(**kwargs)
        self.AddRelatedFace(defender)

    @property
    def defender(self) -> 'Unit2|None':
        return self.defender_internal

class DefenderMessage(DefenderNoneMessage):
    def __init__(self, defender: 'Unit2', **kwargs: Any) -> None:
        super().__init__(defender, **kwargs)

    @property
    def defender(self) -> 'Unit2':
        assert self.defender_internal
        return self.defender_internal

class DamageMessage(Message2):
    def __init__(self, damage: int|None, **kwargs: Any) -> None:
        self.damage_internal = damage
        super().__init__(**kwargs)

    @property
    def will_take_damage(self) -> int:
        assert self.damage_internal != None
        return self.damage_internal

    def UpdateDamageInternal(self, value: int):
        assert self.damage_internal != None
        self.damage_internal += value

################################################################################
#
class CalculateMessage(NoSendResolve):
    def __init__(self, **kwargs: Any) -> None:
        super().__init__(**kwargs)

class CheckNoneMessage(NoSendResolve):
    def __init__(self, by_face: 'CardFace|None', **kwargs: Unpack[WorldKwargs]) -> None:
        self.cause_by: List[Effect] = []
        self.private_by_face: Final = by_face
        super().__init__(**kwargs)
        if self.cause_by != [] and self.private_by_face:
            self.private_by_face.card.ui.unavailable.Add(*self.cause_by)

    def SetCauseBy(self, by_effect: 'Effect'):
        self.cause_by.append(by_effect)

class CheckIfMessage(CheckNoneMessage):
    def __init__(self, by_face: 'CardFace', **kwargs: Any) -> None:
        kwargs.pop('world', None)  # Safely remove 'world' if it exists
        super().__init__(by_face=by_face, world=by_face.card.world, **kwargs)
        self.AddRelatedFace(by_face)

class GettingMessage(NoSendResolve):
    def __init__(self, **kwargs: Unpack[WorldKwargs]) -> None:
        self.return_value = None
        super().__init__(**kwargs)

    def SetReturn(self, val: Any) -> None:
        assert False, f"You need to override this function, {self=}"

################################################################################
#
class CanBeInstead(Message2):
    def __init__(self, **kwargs: Any) -> None:
        self.be_instead_internal = False
        super().__init__(**kwargs)

    def SilentInstead(self):
        if not self.world.is_initializing: # Fix "27087b"
            self.be_instead_internal = True

    def SetBeInstead(self, by_effect: 'Effect'):
        from game.message import Message
        message = Message.WhenMessageBeInstead(self, by_effect)
        message.Send()
        self.SilentInstead()

    @property
    def is_be_instead(self) -> bool:
        return self.be_instead_internal

################################################################################
#
class HasPreEventMessage(Message2):
    def __init__(self, pre_message: 'HasEndEventMessage', **kwargs: Any) -> None:
        self.pre_message: 'Any' = pre_message
        pre_message.next_message = self
        kwargs.pop('world', None)  # Safely remove 'world' if it exists
        super().__init__(**kwargs, world=pre_message.world)
        # assert type(self) == pre_message.CastTo(HasEndEventMessage).end_event
        assert isinstance(self, pre_message.CastTo(HasEndEventMessage).end_event)

TMH = TypeVar("TMH", bound='HasPreEventMessage')

# event is a type
# message is a message that type is event
class HasEndEventMessage(Message2):
    def __init__(self, end_event: 'Type[TMH]', **kwargs: Any) -> None:
        self.next_message: Any
        self.end_event: Final = end_event
        super().__init__(**kwargs)

################################################################################
#
class DeckMessage(Message2):
    def __init__(self, deck: 'Deck') -> None:
        self.deck: Final = deck
        super().__init__(world=deck.world)

################################################################################
#
class CanGainValueMessage(Message2):
    def __init__(self, **kwargs: Any) -> None:
        self.gain_atk = 0
        self.gain_thw = 0
        super().__init__(**kwargs)

    def AddGainedATK(self, value: int):
        self.gain_atk += value

    def AddGainedTHW(self, value: int):
        self.gain_thw += value

################################################################################
#
class CardStateUpdatedMessage(TriggerFaceMessage):
    def __init__(self, trigger: 'CardFace') -> 'None':
        self.updated_face = trigger
        super().__init__(trigger=trigger)

################################################################################
#
class InActivationMessage(Message2):
    def __init__(self, activate_message: 'Message.WhenUnitWouldScheme|None', **kwargs: Any) -> 'None':
        self.activate_message: Final = activate_message
        super().__init__(**kwargs)
