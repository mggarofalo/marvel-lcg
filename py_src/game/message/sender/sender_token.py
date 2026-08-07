from . import *
from typing import Final

class SenderTokenCounter:

    ################################################################################
    # token
    class WhenCardWouldBePlacedToken(TriggerFaceMessage, HasEndEventMessage, CanBeInstead):
        def __init__(self, face: 'CanPlaceToken', num: int, name: 'CardFace.TOKEN', by_effect: 'Effect') -> None:
            from game.message import Message
            self.num = num
            self.token_name: Final = name
            self.by_effect: Final = by_effect
            super().__init__(trigger=face, end_event=Message.AfterCardPlacedToken)

    class AfterCardPlacedToken(TriggerFaceMessage, HasPreEventMessage):
        def __init__(self, face: 'CardFace', num: int, name: 'CardFace.TOKEN', by_effect: 'Effect', message: 'Message.WhenCardWouldBePlacedToken') -> None:
            # from game.message import OnEvent
            self.num: Final = num
            self.token_name: Final = name
            self.by_effect: Final = by_effect
            super().__init__(trigger=face, pre_message=message)
            if num > 0 and (face.IsInPlay() or name != "threat"):
                text = TransText("{face} placed {tokens} '{name}' token ({by_effect})", face=face, tokens=num, name=name, by_effect=by_effect.this)
                self.Present(text, "addcounter", face, by_effect.this)
            # if name == 'acceleration_token':
            #     self.TriggerOnEvent(OnEvent.AccelerationToken)

    class WhenCardWouldRemovedToken(TriggerFaceMessage, HasEndEventMessage, CanBeInstead):
        def __init__(self, face: 'CardFace', num: int|Literal["All"], name: 'CardFace.TOKEN', by_effect: 'Effect') -> None:
            from game.message import Message
            self.would_remove = num if isinstance(num, int) else None
            self.remove_all = num == "All"
            self.token_name: Final = name
            self.by_effect: Final = by_effect
            super().__init__(trigger=face, end_event=Message.AfterCardRemovedToken)

    class AfterCardRemovedToken(TriggerFaceMessage, HasPreEventMessage):
        def __init__(self, face: 'CardFace', removed_num: int, pre_message: 'Message.WhenCardWouldRemovedToken') -> None:
            # from game.message import OnEvent
            self.removed_num: Final = removed_num
            self.token_name: Final = pre_message.token_name
            self.by_effect: Final = pre_message.by_effect
            super().__init__(trigger=face, pre_message=pre_message)
            if removed_num > 0:
                text = TransText("{face} removed {tokens} '{name}' token ({by_effect})", face=face, tokens=removed_num, name=self.token_name, by_effect=self.by_effect.this)
                self.Present(text, "removecounter", face, self.by_effect.this)
            # if self.token_name == 'acceleration_token':
            #     self.TriggerOnEvent(OnEvent.AccelerationToken)

    ################################################################################
    # counter
    class WhenCardWouldBePlacedCounter(TriggerFaceMessage, HasEndEventMessage, CanBeInstead):
        def __init__(self, face: 'CanPlaceCounter', counters: int, name: 'CardFace.COUNTER', by_effect: 'Effect') -> None:
            from game.message import Message
            from game.player import Player
            self.counters = counters
            self.counter_name: Final = name
            self.by_effect: Final = by_effect
            def get_place_player() -> 'Player|None':
                for_message = by_effect.bind_message
                if isinstance(for_message, Message.WhenCardRevealed):
                    player = for_message.GetToPlayer()
                elif isinstance(for_message, Message.WhenCardBecomeBoost):
                    player = for_message.GetToPlayer()
                elif isinstance(for_message, TriggerPlayerMessage):
                    player = for_message.to_player
                elif isinstance(self.by_effect.initiator, Player):
                    player = self.by_effect.initiator
                else:
                    player = None
                return player
            self.place_player: Final = get_place_player()
            self.maximum: int|None = None
            super().__init__(trigger=face, end_event=Message.AfterCardPlacedCounter)
            text = TransText("{face} would place {counters} '{name}' counter ({by_effect})", face=face, counters=counters, name=name, by_effect=by_effect.this)
            self.Present(text, "", face, by_effect.this)

    class AfterCardPlacedCounter(TriggerFaceMessage, HasPreEventMessage):
        def __init__(self, face: 'CardFace', counters: int, name: 'CardFace.COUNTER', by_effect: 'Effect', message: 'Message.WhenCardWouldBePlacedCounter') -> None:
            # from game.message import OnEvent
            self.counters: Final = counters
            self.counter_name: Final = name
            self.by_effect: Final = by_effect
            self.place_player: Final = message.place_player
            super().__init__(trigger=face, pre_message=message)
            if counters > 0:
                text = TransText("{face} placed {counters} '{name}' counter ({by_effect})", face=face, counters=counters, name=name, by_effect=by_effect.this)
                self.Present(text, "addcounter", face, by_effect.this)
            # self.TriggerOnEvent(OnEvent.Counter)

        @property
        def would_place(self) -> 'Message.WhenCardWouldBePlacedCounter':
            return self.pre_message

    class WhenCardWouldRemovedCounter(TriggerFaceMessage, CanBeInstead, HasEndEventMessage):
        def __init__(self, face: 'CardFace', counters: int|Literal["All"], name: 'CardFace.COUNTER', by_effect: 'Effect') -> None:
            from game.message import Message
            self.counters = counters if isinstance(counters, int) else None
            self.remove_all = counters == "All"
            self.counter_name: Final = name
            self.by_effect: Final = by_effect
            super().__init__(trigger=face, end_event=Message.AfterCardRemovedCounter)

    class AfterCardRemovedCounter(TriggerFaceMessage, HasPreEventMessage):
        def __init__(self, face: 'CardFace', removed_counters: int, message: 'Message.WhenCardWouldRemovedCounter') -> None:
            # from game.message import OnEvent
            self.removed_counters: Final = removed_counters
            self.counter_name: Final = message.counter_name
            self.by_effect: Final = message.by_effect
            super().__init__(trigger=face, pre_message=message)
            if removed_counters > 0:
                text = TransText("{face} removed {counters} '{name}' counter ({by_effect})", face=face, counters=removed_counters, name=self.counter_name, by_effect=self.by_effect.this)
                self.Present(text, "removecounter", face, self.by_effect.this)
            # self.TriggerOnEvent(OnEvent.Counter)

