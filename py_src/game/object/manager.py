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

    # `card_dict` is append-only for the life of a game. **Digest-visible** --
    # see `game/world/digest.py`, whose contract is "every card, no exclusions":
    # append-only is what makes that sentence true without a caveat, and it is
    # what lets `game/world/invariants.py` treat a card that is in `card_dict`
    # but in no zone's list as a violation rather than a legal state.
    #
    # There was one exception, `RemoveCard`, called only from `Card.Destroy`,
    # which nothing called. Both are gone under MARVEL-70. Nothing takes a card
    # out of the world: removed-from-game cards sit in `world.area_removed` and
    # keep their entry here, which is also what a C# port should do.
    #
    # `ResetObjects` below is the one thing that empties it, and that is a new
    # game rather than a card leaving one.

    def ResetObjects(self) -> None:
        self.index_dict = ObjectManager.INIT_DICT.copy()
        self.card_dict = {}
        self.effect_dict = {}
        # self.action_effect_dict = {}
        self.paying_effect_dict = {}
        self.message_dict = {}

    def ResetChooseEffect(self) -> None:
        self.index_dict['choose_effect'] = ObjectManager.INIT_DICT['choose_effect']

