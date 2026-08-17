"""Turns a chosen option into the `CommandDescriptor` the engine expects back.

This is legality plumbing, not strategy. Every policy needs the same three
things — pick the minimum legal number of targets, pick a set of resources that
satisfies the cost, and serialise the result the way the browser client does —
so it lives here instead of being re-derived in each policy.

Cost satisfaction is checked with the engine's own `Resources` / `Cost` classes
rather than a reimplementation, so the bot agrees with the rules engine about
what is affordable.
"""

from core import *
from engine.config import ConfigVariables
from engine.log import Log
from engine.device.manager.bot.policy import BotOption, BotTargetCost
from game.scene.replay.operation import CommandDescriptor

CATEGORY_NAME = "BOT"

# `Cost.text_legacy` values that mean "nothing to pay".
NO_COST_TEXTS = ("", "0", "*")

# Off restores the pre-MARVEL-135 planner exactly: every variable cost paid
# with nothing. Kept because it is the behaviour any replay recorded before
# this change was generated under, and because "how much" is a strategy
# question this planner is not the right place to answer well.
PAY_VARIABLE_COST = ConfigVariables.Bool('bot_pay_variable_cost', True)

class BotCommand:

    ################################################################################
    #
    @staticmethod
    def Cancel() -> 'CommandDescriptor':
        """The no-op answer. `Controller.ChoiceOne` reads an empty id as 0 = cancel."""
        return CommandDescriptor()

    @staticmethod
    def IsCancel(command: 'CommandDescriptor') -> bool:
        return str(command.id) in ("", "0")

    @staticmethod
    def IsForcedCancelAllowed(options: List['BotOption']) -> bool:
        """Whether `id == 0` is legal even though the engine said forced.

        Mirrors the assertion in `Controller.ChoiceOne`: a forced decision only
        tolerates a cancel when there is exactly one option and it needs no
        targets. This is the "End Phase" shape.
        """
        return len(options) == 1 and options[0].target_num_range[0] == 0

    @staticmethod
    def ToJson(command: 'CommandDescriptor') -> str:
        """Serialise exactly like `Button.doPost` in `public/js/marvel/buttons.ts`."""
        from engine.lib import Json
        if BotCommand.IsCancel(command):
            return "{}"
        return Json.Dumps({
            'id':        int(command.id),
            'targets':   [int(x) for x in command.targets],
            'resources': [int(x) for x in command.resources],
        })

    ################################################################################
    #
    @staticmethod
    def Build(option: 'BotOption') -> 'CommandDescriptor|None':
        """Build a legal command for `option`, or None if the bot cannot afford it.

        Targets: the first `target_num_range[0]` legal targets. This is the same
        minimum-selection the engine itself uses for a forced effect
        (`PlayerAction`, `fallthrough_effect.context.targets_internal`).
        """
        if not option.is_selectable:
            return None

        min_targets = option.target_num_range[0]
        if len(option.all_legal_targets) < min_targets:
            return None
        targets = option.all_legal_targets[:min_targets]

        resources = BotCommand.BuildPayment(option, targets)
        if resources is None:
            return None

        return CommandDescriptor(
            str(option.id),
            [str(x) for x in targets],
            [str(x) for x in resources],
        )

    @staticmethod
    def BuildPayment(option: 'BotOption', targets: List[int]) -> 'List[int]|None':
        """Greedily pick resources until the cost is met. None if it cannot be met."""
        target_cost = option.GetTargetCost(targets)
        if target_cost is None:
            return []
        if target_cost.cost in NO_COST_TEXTS and not target_cost.payment:
            return []

        try:
            return BotCommand.BuildPaymentInternal(target_cost)
        except Exception as exc:
            # A cost we cannot re-derive from its rendered text. Treat the option
            # as unaffordable so the policy moves on instead of crashing.
            Log.Debug(CATEGORY_NAME, f"Cannot plan payment for {option.name!r} ({target_cost.cost!r}): {exc}")
            return None

    @staticmethod
    def IsSpendItsOwnEffect(target_cost: 'BotTargetCost') -> bool:
        """Is the *size* of this payment the thing the card does?

        Two rules say so, and they are not the same shape:

          * `Variable` -- a printed X. The engine renders the cost as "0"
            because X is 0 until it is chosen, so without this flag the
            planner cannot tell Speed Cyclone from a free card at all.
          * `UpTo` -- "spend up to 3", where the printed number is a ceiling
            rather than a price. This one the planner could always see; it
            just had no reason to look, because zero matches.

        For both, the least legal answer is not a cheap answer -- it is the
        card doing nothing. See MARVEL-135.
        """
        return "Variable" in target_cost.rule or "UpTo" in target_cost.rule

    @staticmethod
    def BuildPaymentInternal(target_cost: 'BotTargetCost') -> 'List[int]|None':
        from game.element.cost import Cost
        from game.element.resources import Resources

        maximal = BotCommand.IsSpendItsOwnEffect(target_cost) and PAY_VARIABLE_COST.value

        if target_cost.cost in NO_COST_TEXTS and not maximal:
            return []

        cost = Cost(
            Cast(Any, target_cost.cost),
            up_to           = ("UpTo" in target_cost.rule) or None,
            same_type       = ("SameType" in target_cost.rule) or None,
            different_type  = ("DifferentType" in target_cost.rule) or None,
            from_hand       = ("FromHand" in target_cost.rule) or None,
            variable        = ("Variable" in target_cost.rule) or None,
        )

        if maximal:
            return BotCommand.BuildMaximalPayment(target_cost, cost)

        paid = Resources.FromText("0")
        chosen: List[int] = []
        if paid.IsMatchCost(cost):
            return chosen

        for payment in target_cost.payment:
            chosen.append(payment.effect_id)
            paid = paid + Resources.FromText(payment.res_text or "0")
            if paid.IsMatchCost(cost):
                return chosen

        return None

    @staticmethod
    def BuildMaximalPayment(target_cost: 'BotTargetCost', cost: 'Cost') -> List[int]:
        """Spend as much as this cost will take, in the order it was offered.

        Deliberately the *whole* offer for a `Variable` cost rather than a
        number picked to match the targets already chosen. Targets are settled
        before payment (MARVEL-133), so bounding the spend by them would make
        it impossible to overpay -- and Everywhere All at Once (58018) prints
        a different effect for overpaying, which nothing could then reach.

        Skips rather than stops on a resource that would break the match: an
        `UpTo` ceiling of 3 with a 2 and a 4 on offer takes the 2, and a
        greedy walk that returned at the first refusal would take neither.
        Engine order throughout, so the plan is deterministic
        (docs/rng-contract.md).

        How much a bot *should* spend is strategy, and a maximum is only the
        least bad answer that is not zero -- `BotCommand` is the shared
        plumbing every policy inherits, so the knob is here and a policy that
        wants to think about it should override rather than tune it.
        """
        from game.element.resources import Resources

        paid = Resources.FromText("0")
        chosen: List[int] = []
        for payment in target_cost.payment:
            more = paid + Resources.FromText(payment.res_text or "0")
            if not more.IsMatchCost(cost):
                continue
            paid = more
            chosen.append(payment.effect_id)
        return chosen

    ################################################################################
    #
    @staticmethod
    def BuildAll(options: List['BotOption']) -> List['CommandDescriptor']:
        """Every option the bot can legally answer with, in engine order."""
        commands: List['CommandDescriptor'] = []
        for option in options:
            command = BotCommand.Build(option)
            if command is not None:
                commands.append(command)
        return commands
