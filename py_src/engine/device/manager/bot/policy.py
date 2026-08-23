"""Decision context and policy contract for the headless `bot` device.

The bot device does not invent a second way to talk to the engine. It receives
exactly what a browser client receives — an `AskOptionPayload` — and answers with
exactly what a browser client posts back: a JSON `CommandDescriptor`.

This module defines the seam between "how a decision reaches the engine" (the
device, which is fixed) and "which decision is made" (the policy, which is
pluggable). A real playing policy belongs behind `BotPolicy`, not inside the
device.

Determinism contract for every implementation of `BotPolicy`:

- no wall-clock time, no dates
- no unseeded randomness; a random policy takes an explicit seed and owns a
  private RNG stream so it never advances the game's `engine.lib.Random`
- no threads
- `BotDecision.world` is READ ONLY; mutating it corrupts the replay
"""

from core import *
import dataclasses
from game.scene.replay.operation import CommandDescriptor

################################################################################
# Parsed view of `AskOptionPayload.options_json`
#
# `Controller.ChoiceOne` sends `Json.Dumps(List[EffectDescriptor])`. JSON turns
# the `Dict[int, Payment]` keys into strings, so the payload has to be normalised
# on the way back in — this mirrors `public/js/marvel/data.ts`, which is the code
# a human client runs.

@dataclass(frozen=True)
class BotPayment:
    """One resource a card/effect can contribute towards a cost."""
    effect_id: int
    res_text: str   # "R" / "B" / "Y" / "G" / "RB" / "-1" (cost reduction) / ""

@dataclass(frozen=True)
class BotTargetCost:
    """What paying for one particular target looks like."""
    # "0" is also what a printed X renders as -- `Variable` in `rule` is the
    # only thing that separates the two. See `BotCommand.IsSpendItsOwnEffect`.
    cost: str               # "0", "3", "R", "RR", "*" (cost ignored), "" (none)
    rule: List[str]         # subset of "UpTo" "FromHand" "SameType"
                            #           "DifferentType" "Variable"
    payment: List[BotPayment]
    # The other way this cost may be paid, empty when there is only one way.
    # Satisfying either is legal, so a planner has to see both -- the engine
    # checks `Resources.IsMatchCost`, which tries `or_res` first.
    or_cost: str = ""
    or_rule: List[str] = dataclasses.field(default_factory=list)

@dataclass(frozen=True)
class BotOption:
    """One selectable effect, as the client sees it."""
    id: int
    name: str
    bind_id: int
    bind_player_id: int
    all_legal_targets: List[int]
    target_num_range: Tuple[int, int]
    target_payment: Dict[int, BotTargetCost]
    select_rule: str
    target_must_include_traits: List[str]
    failure_reason: str
    is_search: bool
    pay_size_is_effect: bool = False

    @property
    def is_selectable(self) -> bool:
        # The client greys out options that carry a failure reason.
        return self.failure_reason == ""

    def GetPaymentKey(self, selected_targets: List[int]) -> int:
        """Port of `EffectDescriptor.getN()` in `public/js/marvel/data.ts`."""
        key = 0
        if selected_targets:
            key = selected_targets[0]
        elif self.all_legal_targets:
            key = self.all_legal_targets[0]
        if key not in self.target_payment:
            key = 0
        return key

    def GetTargetCost(self, selected_targets: List[int]) -> 'BotTargetCost|None':
        return self.target_payment.get(self.GetPaymentKey(selected_targets), None)

################################################################################
#
class BotOptionParser:

    @staticmethod
    def Parse(options_json: str) -> List['BotOption']:
        import json
        if not options_json:
            return []
        raw = json.loads(options_json)
        if not isinstance(raw, list):
            return []
        return [BotOptionParser.ParseOne(Cast(Any, item)) for item in Cast(Any, raw)]

    @staticmethod
    def ParseOne(item: Dict[str, Any]) -> 'BotOption':
        target_range = item.get('target_num_range') or [0, 0]
        while len(target_range) < 2:
            target_range.append(0)

        legal_targets: List[int] = [int(x) for x in (item.get('all_legal_targets') or [])]
        # `data.ts`: an effect that takes at most zero targets shows none.
        if int(target_range[1]) == 0:
            legal_targets = []

        payments: Dict[int, BotTargetCost] = {}
        for key, value in (item.get('target_payment') or {}).items():
            pays: List[BotPayment] = []
            for pay in (value.get('payment') or []):
                # Serialised as `{effect_id: res_text}` with one entry.
                for pay_id, res_text in pay.items():
                    pays.append(BotPayment(int(pay_id), str(res_text)))
            payments[int(key)] = BotTargetCost(
                cost    = str(value.get('cost', "")),
                rule    = [str(x) for x in (value.get('rule') or [])],
                payment = pays,
                or_cost = str(value.get('or_cost', "") or ""),
                or_rule = [str(x) for x in (value.get('or_rule') or [])],
            )

        return BotOption(
            id                          = int(item.get('id', 0)),
            name                        = str(item.get('name', "")),
            bind_id                     = int(item.get('bind_id', 0)),
            bind_player_id              = int(item.get('bind_player_id', 0)),
            all_legal_targets           = legal_targets,
            target_num_range            = (int(target_range[0]), int(target_range[1])),
            target_payment              = payments,
            select_rule                 = str(item.get('select_rule', "") or ""),
            target_must_include_traits  = [str(x) for x in (item.get('target_must_include_traits') or [])],
            failure_reason              = str(item.get('failure_reason', "") or ""),
            is_search                   = bool(item.get('is_search', False)),
            pay_size_is_effect          = bool(item.get('pay_size_is_effect', False)),
        )

################################################################################
#
@dataclass(frozen=True)
class BotDecision:
    """Everything a policy is allowed to see about one pending decision.

    Fields up to `options` are the parsed `AskOptionPayload` — the same bytes a
    browser client is handed. `world` is provided for policies that need real
    game state (MARVEL-10 / MARVEL-14) and must be treated as read only.
    """
    player_id       : int
    step_id         : int   # `InputModule.current_step_id` — the replay step being decided
    attempt         : int   # 0 on the first ask; incremented when the engine rejected the last answer
    event_name      : str
    ability_type    : str
    prompt_text     : str
    can_cancel      : bool  # False means the engine will reject `id == 0`
    options         : List[BotOption]
    replay_input    : str   # recorded answer when replaying, "{}" otherwise
    world           : Any   # 'World|None', READ ONLY

    @property
    def selectable_options(self) -> List[BotOption]:
        return [option for option in self.options if option.is_selectable]

################################################################################
#
class BotStuck(Exception):
    """Raised when a policy has run out of answers the engine will accept.

    The runner turns this into an aborted game rather than letting the engine
    spin forever inside `Controller.ChoiceOne`'s retry loop.
    """

class BotPolicy:
    """Minimal policy contract.

    One method. `Choose` maps a decision context to the command the engine
    expects back: `CommandDescriptor(id, targets, resources)`, where `id == 0`
    (or an empty id) means cancel / no-op.

    Implementations must be deterministic — see the module docstring.
    """

    name: str = "policy"

    def Choose(self, decision: 'BotDecision') -> 'CommandDescriptor':
        raise NotImplementedError

    def OnGameStart(self, seed: int) -> None:
        """Reset per-game policy state. Called before every game the runner starts."""
        pass
