# from typing import TypeAlias
from core import *
from core import Unused

TM = TypeVar("TM", bound='Message2', covariant=True)
TMP = TypeVar("TMP", bound='Message2')
Unused(TM, TMP)

from game.ability import *
from game.effect import *
# ConditionType  : TypeAlias = Callable[['Effect', TM], bool]
# ConditionsType : TypeAlias = List[ConditionType[TM]]
# OperationType  : TypeAlias = Callable[['Effect', TM], None]

ConditionType  = Callable[['Effect', TM], bool]
ConditionsType = List[ConditionType[TM]]
OperationType  = Callable[['Effect', TM], None]


from game.message.message import Message2
Unused(Message2)

from game.message.message_type import CanBeInstead
Unused(CanBeInstead)

from game.message.message_type import AttackerMessageInternal
from game.message.message_type import AttackerNoneOldMessage
from game.message.message_type import AttackerNoneMessage
from game.message.message_type import AttackerMessage
from game.message.message_type import SchemerMessage
from game.message.message_type import ThwarterMessage
from game.message.message_type import DefenderNoneMessage
from game.message.message_type import DefenderMessage
from game.message.message_type import TriggerFaceMessage
from game.message.message_type import TriggerMessage
from game.message.message_type import TriggerNonePlayerMessage
from game.message.message_type import TriggerPlayerMessage
from game.message.message_type import TriggerSchemeMessage
from game.message.message_type import TriggerUnitMessage
from game.message.message_type import TargetsMessage
from game.message.message_type import CanGainValueMessage
Unused(AttackerMessageInternal)
Unused(AttackerNoneOldMessage)
Unused(AttackerNoneMessage)
Unused(AttackerMessage)
Unused(SchemerMessage)
Unused(ThwarterMessage)
Unused(DefenderNoneMessage)
Unused(DefenderMessage)
Unused(TriggerFaceMessage)
Unused(TriggerMessage)
Unused(TriggerNonePlayerMessage)
Unused(TriggerPlayerMessage)
Unused(TriggerSchemeMessage)
Unused(TriggerUnitMessage)
Unused(TargetsMessage)
Unused(CanGainValueMessage)

from game.message.sender.sender import Message
Unused(Message)

from game.message.on_event import OnEvent
Unused(OnEvent)

