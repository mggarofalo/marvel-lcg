from typing import Final
from core import *
from game.ability import *
from game.message import *
from engine.controller import *

class UndoModule:

    class UndoHandle:
        def __init__(self, module: 'UndoModule', card_ids: List[int]|None) -> None:
            self.card_ids: Final = card_ids
            self.module: Final = module
            self.step_id = self.module.manager.replay.current_step_id

        def GetAvailableEffects(self, effects: Sequence['Effect']) -> List['Effect']|None:
            # self.module.manager.skip.skip_to
            # step_id = self.module.manager.replay.current_step_id
            if self.step_id == 59:
                pass

            if self.card_ids == None:
                return None
            return [x for x in effects if x.this.card.object_id in self.card_ids]

    def __init__(self, manager: 'ControllerManager') -> None:
        self.next_step: int = 0
        self.last_step: int = 0
        self.manager: Final = manager

        # message_id, [card_ids]
        self.undo_effect_card_cache: Dict[int, Set[int]] = {}

        self.debug_message_id: Dict[int, str] = {}

    def PushNewStep(self, step: int):
        self.last_step = self.next_step
        self.next_step = step

    def UpdateLastStep(self):
        self.last_step = self.next_step
        # self.next_step = step

    def Clean(self, by_undo: bool):
        self.last_step = 0
        self.next_step = 0

        if not by_undo:
            self.undo_effect_card_cache = {}
            self.debug_message_id = {}

    def PushFastUndo(self, message: 'Message2', effects: List['Effect']):
        from game.message.message_type import CheckNoneMessage
        if isinstance(message, CheckNoneMessage):
            return

        object_id = message.object_id
        if object_id not in self.debug_message_id:
            self.debug_message_id[object_id] = message.name
        else:
            assert self.debug_message_id[object_id] == message.name

        if object_id not in self.undo_effect_card_cache:
            self.undo_effect_card_cache[object_id] = set([])
        self.undo_effect_card_cache[object_id] |= set([x.this.card.object_id for x in effects])

    def GetFastUndoHandle(self, message: 'Message2') -> 'UndoHandle|None':
        from game.message.message_type import CheckNoneMessage
        if isinstance(message, CheckNoneMessage):
            return None
        elif message.object_id not in self.undo_effect_card_cache:
            return None
        elif self.DoNotCheckFastUndo():
            return None
        else:
            return UndoModule.UndoHandle(self, list(self.undo_effect_card_cache[message.object_id]))

    def DoNotCheckFastUndo(self) -> bool:
        controller_manager = self.manager
        if not controller_manager.skip.is_skipping:
            return True

        if controller_manager.replay.replay_step_id >= controller_manager.skip.skip_to:
            return True

        return False

