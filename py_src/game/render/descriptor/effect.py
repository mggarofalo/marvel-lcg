from core import *

# data.ts
@dataclass
class EffectDescriptor:

    @dataclass
    class Payment:
        cost: str
        payment: List[Dict[int, str]] # `effect_id` `res_text`
        rule: List[str]

    id: int                         # game object id
    name: str
    bind_id: int                    # link `CardState.id`
    bind_player_id: int             # [0,1,2,3]
    all_legal_targets: List[int]    # link `CardState.id`
    target_num_range: List[int]     # target [min, max] number
    target_payment: Dict[int, Payment]

    select_rule: str
    select_rule_param: Tuple[int, int]
    target_must_include_traits: List[str] # For "26035"

    failure_reason: str             # not null if fail
    is_search: bool
    pay_size_is_effect: bool        # selecting more is the size of a cost's effect

    # is_ex_effect check `AskOption`
