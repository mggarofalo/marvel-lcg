"""The `When` clause: a `BotPolicy` that plays a scripted list of actions.

The policy is the whole of the When. It sees the same `AskOptionPayload` a
browser client sees and answers with the same `CommandDescriptor` a browser
client posts, so a spec run goes down the ordinary `Controller.ChoiceOne`
path -- validation, CRC, `replay.Push` -- exactly like a human game.

Three phases:

1. **Script active.** The next `WhenStep` is matched against the selectable
   options. A match is answered; anything else is answered so the engine keeps
   moving -- first legal option for a forced decision (the engine is resolving
   something and will not take no for an answer), cancel for an optional one.
2. **Halt.** Once the script is exhausted, the first *cancellable* decision is
   the stopping point: a cancellable decision means nothing is pending and the
   scripted action has fully resolved. The state is captured there. Forced
   decisions still get a first-legal answer so pending effects finish first.
3. **Stop.** Halting is `world.game_over.SetExit()` plus a cancel -- the same
   graceful stop `BotDeviceManager.StopIfOverMaxSteps` uses. Raising out of
   `Choose` does *not* work: the engine catches exceptions while broadcasting
   messages, logs them, and carries on playing.

Determinism, per the contract in `engine/device/manager/bot/policy.py`: no
wall clock, no randomness at all, no threads, and `decision.world` is read only
apart from the one `SetExit` that stops the game.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Tuple

from engine.device.manager.bot.command import BotCommand
from engine.device.manager.bot.policy import BotPolicy
from tools.spec.case import WhenStep
from tools.spec.resolve import CardRefError, DescribeOption, ResolveCard
from tools.spec.state import Capture, StateView


@dataclass
class DecisionRecord:
    """One decision the policy answered, kept for the triage queue."""

    step_id: int
    event_name: str
    ability_type: str
    can_cancel: bool
    options: Tuple[str, ...]
    answered: str

    def Describe(self) -> str:
        listing = "; ".join(self.options) if self.options else "no options"
        return (f"#{self.step_id} {self.event_name} ({self.ability_type}): "
                f"{listing} -> {self.answered}")


class ScriptFailure(Exception):
    """A When step the engine never offered. Carries the decisions we saw."""

    def __init__(self, message: str, records: "List[DecisionRecord]") -> None:
        super().__init__(message)
        self.records = records


@dataclass
class ScriptedPolicy(BotPolicy):
    """Plays `steps`, then halts and captures the board."""

    steps: Tuple[WhenStep, ...] = ()
    max_decisions: int = 200

    name: str = "spec"

    index: int = 0
    records: List[DecisionRecord] = field(default_factory=list)
    state: Optional[StateView] = None
    halted: bool = False
    failure: str = ""
    """Set when a step was never offered, or the decision budget ran out."""

    ############################################################################
    #
    @property
    def completed(self) -> bool:
        return self.index >= len(self.steps)

    def OnGameStart(self, seed: int) -> None:
        self.index = 0
        self.records = []
        self.state = None
        self.halted = False
        self.failure = ""

    ############################################################################
    #
    def Choose(self, decision: Any) -> Any:
        if self.halted:
            # The engine is unwinding; decline whatever it still asks.
            return BotCommand.Cancel()

        if len(self.records) >= self.max_decisions:
            # The budget can trip in either phase, including while unwinding
            # after the last When step -- so the message must not assume there
            # is a next step to name. An IndexError here would be swallowed by
            # the engine's message-broadcast error handling and the game would
            # simply keep playing, defeating the backstop entirely.
            if self.completed:
                detail = "while unwinding after the last When step"
            else:
                detail = (f"with {len(self.steps) - self.index} When step(s) unplayed "
                          f"(next: {self.steps[self.index].Describe()})")
            self.Fail(f"gave up after {self.max_decisions} decisions {detail}", decision)
            return self.Halt(decision)

        if self.completed:
            if decision.can_cancel:
                return self.Halt(decision)
            return self.Answer(decision, self.FirstLegal(decision), "first legal (unwinding)")

        step = self.steps[self.index]

        if step.pass_priority:
            if not decision.can_cancel:
                # "pass" against a forced decision is not a pass; the engine
                # will reject it. Say so rather than looping on the retry.
                self.Fail(
                    f"When step {self.index + 1} says pass, but the engine is "
                    f"forcing a choice at '{decision.event_name}'",
                    decision)
                return self.Halt(decision)
            self.index += 1
            return self.Answer(decision, BotCommand.Cancel(), "pass")

        command, why = self.BuildFor(step, decision)
        if command is not None:
            self.index += 1
            return self.Answer(decision, command, f"step {self.index}: {step.Describe()}")

        # Not this decision. Keep the engine moving without ending the turn on
        # a forced prompt, and record what was on offer so a "never became
        # available" failure can name it.
        if decision.can_cancel:
            return self.Answer(decision, BotCommand.Cancel(), f"declined ({why})")
        return self.Answer(decision, self.FirstLegal(decision), f"first legal ({why})")

    ############################################################################
    #
    def BuildFor(self, step: WhenStep, decision: Any) -> Tuple[Any, str]:
        """The command for `step`, or (None, why not)."""
        from game.scene.replay.operation import CommandDescriptor

        # Resolve the bound card once, up front. Doing it inside the filter
        # would turn "this name means two cards" into a bare "no option
        # matches", hiding the one failure the author can actually act on.
        if step.card and decision.world is not None:
            try:
                ResolveCard(decision.world, step.card)
            except CardRefError as exc:
                return None, str(exc)

        matched = [option for option in decision.selectable_options
                   if self.Matches(step, option, decision.world)]
        if not matched:
            return None, f"no option matches {step.Describe()}"

        reasons: List[str] = []
        for option in matched:
            targets, error = self.ChooseTargets(step, option, decision.world)
            if error:
                reasons.append(error)
                continue

            # Cost depends on which target was chosen, so the payment has to be
            # planned against the spec's targets rather than `BotCommand.Build`'s
            # default first-N pick.
            resources = BotCommand.BuildPayment(option, targets)
            if resources is None:
                reasons.append(f"{option.name} is offered but cannot be paid for")
                continue

            return CommandDescriptor(
                str(option.id),
                [str(x) for x in targets],
                [str(x) for x in resources],
            ), ""

        return None, "; ".join(reasons) or f"no usable option for {step.Describe()}"

    def ChooseTargets(self, step: WhenStep, option: Any, world: Any) -> Tuple[List[int], str]:
        """Target object ids for `option`, or (empty, why not).

        With no targets in the spec the engine's own minimum selection is used,
        but only when there is nothing to choose between. An effect with two
        legal targets and a spec that names neither is under-specified, and a
        harness that guesses would produce a scenario whose result depends on
        engine ordering rather than on the card.
        """
        legal = [int(x) for x in option.all_legal_targets]
        low, high = int(option.target_num_range[0]), int(option.target_num_range[1])

        if step.targets:
            chosen: List[int] = []
            for ref in step.targets:
                try:
                    card = ResolveCard(world, ref) if world is not None else None
                except CardRefError as exc:
                    return [], str(exc)
                if card is None:
                    return [], "no world to resolve targets against"
                if int(card.object_id) not in legal:
                    listing = ", ".join(self.LabelTargets(world, option)) or "nothing"
                    return [], (f"{ref} is not a legal target for {option.name}; "
                                f"legal targets are {listing}")
                chosen.append(int(card.object_id))
            if len(chosen) < low or (high and len(chosen) > high):
                return [], (f"{option.name} takes {low}..{high} target(s), "
                            f"the spec gave {len(chosen)}")
            return chosen, ""

        if low == 0:
            return [], ""
        if len(legal) < low:
            return [], f"{option.name} needs {low} target(s) but only {len(legal)} are legal"
        if len(legal) > low:
            listing = ", ".join(self.LabelTargets(world, option))
            return [], (f"{option.name} has {len(legal)} legal targets ({listing}); "
                        f"say which with 'targeting \"<card>\"'")
        return legal[:low], ""

    def Matches(self, step: WhenStep, option: Any, world: Any) -> bool:
        if step.option and str(option.name).strip().lower() != step.option.strip().lower():
            return False
        if step.card:
            if not world:
                return False
            try:
                wanted = ResolveCard(world, step.card)
            except CardRefError:
                return False
            if int(option.bind_id) != int(wanted.object_id):
                return False
        return True

    def LabelTargets(self, world: Any, option: Any) -> List[str]:
        from tools.spec.resolve import CardIndex, Label
        index = CardIndex(world)
        labels: List[str] = []
        for target_id in option.all_legal_targets:
            card = index.get(int(target_id))
            labels.append(Label(card) if card is not None else str(target_id))
        return labels

    ############################################################################
    #
    def FirstLegal(self, decision: Any) -> Any:
        commands = BotCommand.BuildAll(decision.selectable_options)
        if commands:
            return commands[0]
        return BotCommand.Cancel()

    def Answer(self, decision: Any, command: Any, why: str) -> Any:
        self.records.append(DecisionRecord(
            step_id=int(decision.step_id),
            event_name=str(decision.event_name),
            ability_type=str(decision.ability_type),
            can_cancel=bool(decision.can_cancel),
            options=tuple(self.DescribeOptions(decision)),
            answered=why,
        ))
        return command

    def DescribeOptions(self, decision: Any) -> List[str]:
        if decision.world is None:
            return [str(option.name) for option in decision.options]
        return [DescribeOption(decision.world, option) for option in decision.options]

    def Halt(self, decision: Any) -> Any:
        """Capture the board and stop the game the way the engine expects."""
        self.halted = True
        if decision.world is not None:
            self.state = Capture(decision.world)
            decision.world.game_over.SetExit()
        return BotCommand.Cancel()

    def Fail(self, message: str, decision: Any) -> None:
        if not self.failure:
            offered = "; ".join(self.DescribeOptions(decision)) or "no options"
            self.failure = (f"{message}\n"
                            f"     at '{decision.event_name}' the engine offered: {offered}")


def DescribeTrail(records: "List[DecisionRecord]", limit: int = 12) -> str:
    """The last few decisions, for a failure that needs context."""
    tail = records[-limit:]
    lines = [f"     {record.Describe()}" for record in tail]
    if len(records) > len(tail):
        lines.insert(0, f"     ... {len(records) - len(tail)} earlier decision(s)")
    return "\n".join(lines)
