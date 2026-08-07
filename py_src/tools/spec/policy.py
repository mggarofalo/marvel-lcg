"""The transcript: a `BotPolicy` that plays a scenario's beats in order.

The policy is the whole of the `When`/`Then` interleaving. It sees the same
`AskOptionPayload` a browser client sees and answers with the same
`CommandDescriptor` a browser client posts, so a spec run goes down the ordinary
`Controller.ChoiceOne` path -- validation, CRC, `replay.Push` -- exactly like a
human game. No new engine seam.

**Assertions live here, not after the run.** `decision.world` is the state right
after the previous decision fully resolved, and that is the only point where a
`Then` between two `When`s is observable. Once the engine unwinds, the
intermediate states are gone.

On each `Choose(decision)`:

1. Drain leading assertion beats against `decision.world`.
2. If the next beat is a prompt expectation, check it against this decision and
   consume it -- the beat after it answers this same prompt.
3. Answer with the next action beat.
4. When the queue empties, capture the board, `SetExit()` and cancel.

Leftovers are judged at the end: assertions still queued are evaluated against
the final world, a queued `no prompt` passes (the engine never asked again), and
a queued **action** fails -- the game ended before that step could run, which is
what catches a transcript that has drifted out of sync with the engine.

**Nothing is auto-answered.** A decision the transcript does not account for is
a failure, not something to paper over with the first legal option. Answering it
silently would make the scenario's result depend on the harness's taste rather
than on the card, and would hide exactly the mid-resolution choices a spec
exists to pin down.

Determinism, per the contract in `engine/device/manager/bot/policy.py`: no wall
clock, no randomness at all, no threads, and `decision.world` is read only apart
from the one `SetExit` that stops the game.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, List, Optional, Tuple

from engine.device.manager.bot.command import BotCommand
from engine.device.manager.bot.policy import BotPolicy
from tools.spec.assertions import AssertionResult, Evaluate
from tools.spec.case import Beat, NoPromptStep, PromptStep, ThenStep, WhenStep
from tools.spec.resolve import (
    CardRefError, DescribeOption, NormaliseLabel, ResolveCard)
from tools.spec.state import Capture, StateView

# `event_name` discriminates prompt kinds. A mid-resolution ask is the engine
# asking a question *inside* a card's resolution; the turn menu coming back
# around is not. This is what makes "I am not prompted again" checkable.
TURN_LEVEL_EVENTS = ("WhenPlayerInTurn", "End Turn", "End Phase")


def IsMidResolution(decision: Any) -> bool:
    return str(decision.event_name) not in TURN_LEVEL_EVENTS


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


@dataclass
class TranscriptPolicy(BotPolicy):
    """Plays `beats` against the engine, in order, asserting as it goes."""

    beats: Tuple[Beat, ...] = ()
    max_decisions: int = 200

    name: str = "spec"

    index: int = 0
    records: List[DecisionRecord] = field(default_factory=list)
    results: List[AssertionResult] = field(default_factory=list)
    state: Optional[StateView] = None
    halted: bool = False
    failure: str = ""
    """Set when the transcript and the engine stopped agreeing."""

    ############################################################################
    #
    @property
    def completed(self) -> bool:
        return self.index >= len(self.beats)

    def Peek(self) -> Optional[Beat]:
        return None if self.completed else self.beats[self.index]

    def OnGameStart(self, seed: int) -> None:
        self.index = 0
        self.records = []
        self.results = []
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
            # The budget can trip in either phase, so the message must not
            # assume there is a next beat to name. An IndexError here would be
            # swallowed by the engine's message-broadcast error handling and
            # play would simply continue, disabling the backstop entirely.
            remaining = len(self.beats) - self.index
            detail = (f"with {remaining} beat(s) unplayed" if remaining
                      else "while unwinding after the last beat")
            self.Fail(f"gave up after {self.max_decisions} decisions {detail}", decision)
            return self.Halt(decision)

        self.DrainAssertions(decision)

        beat = self.Peek()

        if beat is None:
            if IsMidResolution(decision):
                # The transcript ran out while the engine was still resolving
                # something. That is an incomplete scenario, not a disagreement
                # about an outcome: whatever the engine does next was chosen by
                # nobody. Saying so is the whole reason a scenario is a
                # transcript rather than a batch of actions.
                self.Fail(
                    f"the transcript ended while the engine was still asking "
                    f"'{decision.event_name}', offering {self.Offered(decision)}",
                    decision)
            return self.Halt(decision)

        if isinstance(beat, NoPromptStep):
            # We are being asked something. Whether that breaks the claim
            # depends on what kind of question it is.
            if IsMidResolution(decision):
                self.Record(beat, False,
                            f"expected no further prompt, but the engine asked "
                            f"'{decision.event_name}' offering "
                            f"{self.Offered(decision)}")
            else:
                self.Record(beat, True)
            self.index += 1
            return self.Halt(decision)

        if isinstance(beat, PromptStep):
            self.CheckPrompt(beat, decision)
            self.index += 1
            beat = self.Peek()
            if beat is None:
                return self.Halt(decision)

        if not isinstance(beat, WhenStep):
            # A `Then` cannot survive DrainAssertions; anything else here is a
            # beat kind this method does not know about.
            self.Fail(f"unexpected beat {beat.Describe()!r} at a decision", decision)
            return self.Halt(decision)

        return self.Act(beat, decision)

    ############################################################################
    #
    def DrainAssertions(self, decision: Any) -> None:
        """Evaluate every `Then` queued before the next action or prompt.

        `decision.world` is the board as it stands after the previous decision
        resolved. Nothing else in the run can see it.
        """
        if decision.world is None or not isinstance(self.Peek(), ThenStep):
            return
        # One snapshot for the whole run of assertions: they all describe the
        # same moment, and capturing per assertion would walk every card in the
        # world once per `Then`.
        board = Capture(decision.world)
        while isinstance(self.Peek(), ThenStep):
            self.results.append(Evaluate(board, self.beats[self.index]))
            self.index += 1

    def CheckPrompt(self, beat: PromptStep, decision: Any) -> None:
        expected = sorted(NormaliseLabel(option) for option in beat.options)
        actual = sorted(NormaliseLabel(option.name)
                        for option in decision.selectable_options)
        if expected == actual:
            self.Record(beat, True)
            return
        missing = [o for o in expected if o not in actual]
        extra = [o for o in actual if o not in expected]
        parts: List[str] = []
        if missing:
            parts.append(f"missing {', '.join(repr(o) for o in missing)}")
        if extra:
            parts.append(f"unexpected {', '.join(repr(o) for o in extra)}")
        self.Record(beat, False,
                    f"the engine offered {self.Offered(decision)} ({'; '.join(parts)})")

    def Act(self, beat: WhenStep, decision: Any) -> Any:
        if beat.pass_priority:
            if not decision.can_cancel:
                self.Fail(
                    f"the transcript says {beat.Describe()!r}, but the engine is "
                    f"forcing a choice at '{decision.event_name}'", decision)
                return self.Halt(decision)
            self.index += 1
            return self.Answer(decision, BotCommand.Cancel(), beat.Describe())

        command, why = self.BuildFor(beat, decision)
        if command is None:
            # No guessing. A decision the transcript does not account for is a
            # disagreement between the spec and the engine, and saying so is the
            # entire job.
            self.Fail(f"{beat.Describe()} could not be played: {why}", decision)
            return self.Halt(decision)

        self.index += 1
        return self.Answer(decision, command, beat.Describe())

    ############################################################################
    #
    def BuildFor(self, beat: WhenStep, decision: Any) -> Tuple[Any, str]:
        """The command for `beat`, or (None, why not)."""
        from game.scene.replay.operation import CommandDescriptor

        # Resolve the bound card once, up front. Doing it inside the filter
        # would turn "this name means two cards" into a bare "no option
        # matches", hiding the one failure the author can act on.
        if beat.card and decision.world is not None:
            try:
                ResolveCard(decision.world, beat.card)
            except CardRefError as exc:
                return None, str(exc)

        matched = [option for option in decision.selectable_options
                   if self.Matches(beat, option, decision.world)]
        if not matched:
            return None, f"the engine offered {self.Offered(decision)}"

        reasons: List[str] = []
        for option in matched:
            targets, error = self.ChooseTargets(beat, option, decision.world)
            if error:
                reasons.append(error)
                continue

            # Cost depends on which target was chosen, so payment has to be
            # planned against the transcript's targets rather than
            # `BotCommand.Build`'s default first-N pick.
            resources = BotCommand.BuildPayment(option, targets)
            if resources is None:
                reasons.append(f"{option.name} is offered but cannot be paid for")
                continue

            return CommandDescriptor(
                str(option.id),
                [str(x) for x in targets],
                [str(x) for x in resources],
            ), ""

        return None, "; ".join(reasons) or "no usable option"

    def Matches(self, beat: WhenStep, option: Any, world: Any) -> bool:
        if beat.option:
            if NormaliseLabel(option.name) != NormaliseLabel(beat.option):
                return False
        if beat.card:
            if not world:
                return False
            try:
                wanted = ResolveCard(world, beat.card)
            except CardRefError:
                return False
            if int(option.bind_id) != int(wanted.object_id):
                return False
        return True

    def ChooseTargets(self, beat: WhenStep, option: Any, world: Any) -> Tuple[List[int], str]:
        """Target object ids for `option`, or (empty, why not).

        With no targets in the transcript the engine's own minimum selection is
        used, but only when there is nothing to choose between. A single legal
        target is auto-selected by the engine itself and produces no prompt, so
        naming it would be noise; two legal targets and a transcript that names
        neither is under-specified, and guessing would make the result depend on
        engine ordering rather than on the card.
        """
        legal = [int(x) for x in option.all_legal_targets]
        low, high = int(option.target_num_range[0]), int(option.target_num_range[1])

        if beat.targets:
            chosen: List[int] = []
            for ref in beat.targets:
                if world is None:
                    return [], "no world to resolve targets against"
                try:
                    card = ResolveCard(world, ref)
                except CardRefError as exc:
                    return [], str(exc)
                if int(card.object_id) not in legal:
                    listing = ", ".join(self.LabelTargets(world, option)) or "nothing"
                    return [], (f"{ref} is not a legal target for {option.name}; "
                                f"legal targets are {listing}")
                chosen.append(int(card.object_id))
            if len(chosen) < low or (high and len(chosen) > high):
                return [], (f"{option.name} takes {low}..{high} target(s), "
                            f"the transcript gave {len(chosen)}")
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

    def LabelTargets(self, world: Any, option: Any) -> List[str]:
        from tools.spec.resolve import CardIndex, Label
        if world is None:
            return []
        index = CardIndex(world)
        labels: List[str] = []
        for target_id in option.all_legal_targets:
            card = index.get(int(target_id))
            labels.append(Label(card) if card is not None else str(target_id))
        return labels

    ############################################################################
    #
    def Finish(self, world: Any) -> None:
        """Judge whatever is still queued once the engine has stopped asking."""
        if self.state is None and world is not None:
            self.state = Capture(world)

        while not self.completed:
            beat = self.beats[self.index]
            self.index += 1
            if isinstance(beat, NoPromptStep):
                # The engine never asked again, which is the claim.
                self.Record(beat, True)
            elif isinstance(beat, ThenStep):
                if self.state is not None:
                    self.results.append(Evaluate(self.state, beat))
            elif isinstance(beat, PromptStep):
                self.Record(beat, False,
                            "the game ended before the engine asked this")
            else:
                self.Record(beat, False,
                            "the game ended before this step could be played")

    ############################################################################
    #
    def Offered(self, decision: Any) -> str:
        listing = self.DescribeOptions(decision)
        return "; ".join(listing) if listing else "nothing"

    def DescribeOptions(self, decision: Any) -> List[str]:
        if decision.world is None:
            return [str(option.name) for option in decision.options]
        return [DescribeOption(decision.world, option) for option in decision.options]

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

    def Record(self, beat: Beat, passed: bool, message: str = "") -> None:
        self.results.append(AssertionResult(
            step=ThenStep("transcript", "beat", beat.Describe()),
            passed=passed,
            message=message,
            label=beat.Describe(),
        ))

    def Halt(self, decision: Any) -> Any:
        """Capture the board and stop the game the way the engine expects."""
        self.halted = True
        if decision.world is not None:
            self.state = Capture(decision.world)
            self.Finish(decision.world)
            decision.world.game_over.SetExit()
        return BotCommand.Cancel()

    def Fail(self, message: str, decision: Any) -> None:
        if not self.failure:
            self.failure = (f"{message}\n"
                            f"     at '{decision.event_name}' "
                            f"(step {decision.step_id})")


def DescribeTrail(records: "List[DecisionRecord]", limit: int = 12) -> str:
    """The last few decisions, for a failure that needs context."""
    tail = records[-limit:]
    lines = [f"     {record.Describe()}" for record in tail]
    if len(records) > len(tail):
        lines.insert(0, f"     ... {len(records) - len(tail)} earlier decision(s)")
    return "\n".join(lines)
