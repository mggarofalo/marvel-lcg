from core import *

from game.object import Object

class ObjectManager:

    OBJECT_CATEGORY = Literal[
        'card',
        'effect',
        'player',
        'scenario',
        'forced_effect',
        # 'action_effect',
        'paying_effect',
        'message',
        'check_message',
        'choose_effect',
        'game_area',
        'deck'
    ]

    INIT_DICT: Dict[OBJECT_CATEGORY, int] = {
        "card": -1,
        "effect": 0,
        "message": 0,
        'check_message': 0,
        "player": 0,
        "scenario": 0,
        "forced_effect": 0,
        # 'action_effect': 0,
        "paying_effect": 0,
        "choose_effect": 0,
        "game_area": 0,
        "deck": 0,
    }

    def __init__(self) -> None:
        # `replay_input.effect.id == -1` means it is a debug command
        # `replay_input.effect.id == 0` means select nothing
        from game.card import Card
        from game.effect import Effect
        from game.message import Message2

        self.index_dict: Dict[ObjectManager.OBJECT_CATEGORY, int]

        self.card_dict: Dict[int, 'Card'] = {}
        self.effect_dict: Dict[int, 'Effect'] = {}
        # self.action_effect_dict: Dict[int, 'Effect'] = {}
        self.paying_effect_dict: Dict[int, 'Effect'] = {}
        self.message_dict: Dict[int, 'Message2'] = {}

        self.ResetObjects()

    def AddObject(self, category: 'ObjectManager.OBJECT_CATEGORY', object: 'Object') -> int:
        from game.card import Card
        from game.effect import Effect
        from game.message import Message2

        self.index_dict[category] += 1
        object_id = self.index_dict[category]

        if category == 'card':
            self.card_dict[object_id] = Cast(Card, object)
        if category == 'effect':
            self.effect_dict[object_id] = Cast(Effect, object)
        # if category == 'action_effect':
        #     self.action_effect_dict[object_id] = Cast(Effect, object)
        if category == 'paying_effect':
            self.paying_effect_dict[object_id] = Cast(Effect, object)
        if category == 'message':
            self.message_dict[object_id] = Cast(Message2, object)

        return object_id

    def RemoveCard(self, object_id: int) -> None:
        """Forget a card entirely. **Digest-visible** -- see `game/world/digest.py`.

        The digest walks `card_dict`, and its contract is "every card, no
        exclusions", so this is the only way a card can stop being described.
        `Card.Destroy` is the one caller: before MARVEL-50 it removed the card
        from its area and unregistered its effects but left it here, still
        pointing at the area it had just been taken out of, so the digest went
        on reporting a destroyed card in whatever zone the stale pointer named.

        **The id is not released.** `index_dict` is only ever incremented, so a
        later card cannot be allocated the id of a destroyed one. That is part
        of the object-id contract a port has to reproduce -- see
        `docs/state-digest-v2.md`.

        Removing an id that is not present is a programming error rather than a
        no-op: it means the caller believes it destroyed something twice.
        """
        assert object_id in self.card_dict, f"{object_id=} is not a live card"
        del self.card_dict[object_id]

    def ResetObjects(self) -> None:
        self.index_dict = ObjectManager.INIT_DICT.copy()
        self.card_dict = {}
        self.effect_dict = {}
        # self.action_effect_dict = {}
        self.paying_effect_dict = {}
        self.message_dict = {}

    def ResetChooseEffect(self) -> None:
        self.index_dict['choose_effect'] = ObjectManager.INIT_DICT['choose_effect']

