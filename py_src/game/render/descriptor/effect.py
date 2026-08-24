from core import *
from dataclasses import field

# data.ts
@dataclass
class EffectDescriptor:

    @dataclass
    class Payment:
        cost: str
        payment: List[Dict[int, str]] # `effect_id` `res_text`
        rule: List[str]
        # An alternative cost ("spend a [mental] resource *or* 2 of any type").
        # Additive fields: `cost`/`rule` keep describing the primary reading, so
        # a reader that does not know about these is unaffected. A reader that
        # plans a payment needs them -- see `BotCommand.BuildPaymentInternal`.
        or_cost: str = ""
        or_rule: List[str] = field(default_factory=list)

    id: int                         # game object id
    name: str
    bind_id: int                    # link `CardState.id`
    bind_player_id: int             # [0,1,2,3]
    all_legal_targets: List[int]    # link `CardState.id`
    target_num_range: List[int]     # target [min, max] number
    target_payment: Dict[int, Payment]

    select_rule: str
    select_rule_param: Tuple[int, int]
    # Complete legal selections, when the select rule groups its candidates
    # instead of taking them all. `VillainAndMinionsEngagedSamePlayer` pools
    # every player's minions but accepts exactly one villain plus one player's
    # whole group, so the flat `all_legal_targets` cannot express a legal
    # choice and `target_num_range` is not a count anyone can act on. Empty
    # for every ordinary rule. See `SelectorRule.AfterSelectTargets`.
    target_groups: List[List[int]]
    target_must_include_traits: List[str] # For "26035"

    failure_reason: str             # not null if fail
    is_search: bool
    pay_size_is_effect: bool        # selecting more is the size of a cost's effect

    # is_ex_effect check `AskOption`
