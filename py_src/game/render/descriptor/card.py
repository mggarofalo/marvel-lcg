from core import *

# world_descriptor.ts
@dataclass
class CardDescriptor:
    id                  : int               # game object id
    is_ready            : bool
    is_face_up          : bool
    visible_for_players : List[int]         # which player can visit this card
    bind_object_id      : int               # is attach or inventory
    effect_by_cards     : List[int]
    effect_to_cards     : List[int]
    game_area           : int
    name                : str               # face up name
    info                : Dict[str, int]
    traits              : Dict[str, int]    # the `int` param should not be use
    buffs               : Dict[str, str]
    card_id             : str               # current face card_id
    pic_id              : str
    down_card_ids       : List[str]         # down face card_id
    effects             : List[int]         # link `EffectState.id`, only non-force
    resources           : List[int]         # link `EffectState.id`, only resources
    card_type           : str
    cost                : int
    revision            : int          # changes when the rendered card changes
    is_new              : bool
    is_action           : bool
