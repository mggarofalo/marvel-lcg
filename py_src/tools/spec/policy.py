"""The transcript: a `BotPolicy` that plays a scenario's beats in order.

The policy is the whole of the `When`/`Then` interleaving. It sees the same
`AskOptionPayload` a browser client sees and answers with the same
`CommandDescriptor` a browser client posts, so a spec run goes down the ordinary
`Controller.ChoiceOne` path -- validation, digest, `replay.Push` -- exactly like a
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
from tools.spec.case import (
    Beat, CannotStep, LimitStep, MinimumStep, NoPromptStep, NotOfferedStep,
    PromptStep, TargetsStep, ThenStep, WhenStep)
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
    pending_exact_payments: List[Tuple[Any, int, str]] = field(default_factory=list)
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
        self.pending_exact_payments = []
        self.state = None
        self.halted = False
        self.failure = ""

    ############################################################################
    #
    def Choose(self, decision: Any) -> Any:
        if self.halted:
            # The engine is unwinding; decline whatever it still asks.
            return BotCommand.Cancel()

        self.SettleExactPayments(decision)
        if self.failure:
            return self.Halt(decision)

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
            # Drain again: assertions written *after* the prompt table describe
            # this same decision, and "these options, and this one will not take
            # that target" is the natural way to write a restriction down.
            self.DrainAssertions(decision)
            beat = self.Peek()
            if beat is None:
                return self.Halt(decision)

        if not isinstance(beat, WhenStep):
            # An assertion cannot survive DrainAssertions; anything else here is
            # a beat kind this method does not know about.
            self.Fail(f"unexpected beat {beat.Describe()!r} at a decision", decision)
            return self.Halt(decision)

        return self.Act(beat, decision)

    ############################################################################
    #
    def DrainAssertions(self, decision: Any) -> None:
        """Evaluate every assertion queued before the next action or prompt.

        Two kinds, and they read against different things. A `Then` describes
        the board, so it is evaluated against a snapshot of `decision.world` --
        the state as it stands after the previous decision resolved, which
        nothing else in the run can see. A `cannot` describes the *decision*,
        so it is checked against the live options and never sees the snapshot.

        They drain together because a scenario interleaves them freely and the
        order it writes them in should not change what they mean.
        """
        board: Optional[StateView] = None
        while isinstance(self.Peek(),
                         (ThenStep, NotOfferedStep, CannotStep, TargetsStep,
                          MinimumStep, LimitStep)):
            beat = self.beats[self.index]
            if isinstance(beat, NotOfferedStep):
                self.CheckNotOffered(beat, decision)
            elif isinstance(beat, CannotStep):
                self.CheckCannot(beat, decision)
            elif isinstance(beat, TargetsStep):
                self.CheckTargets(beat, decision)
            elif isinstance(beat, LimitStep):
                self.CheckLimit(beat, decision)
            elif isinstance(beat, MinimumStep):
                self.CheckMinimum(beat, decision)
            else:
                if decision.world is None:
                    # No board to read. Leave it queued rather than passing it:
                    # `Finish` judges the leftovers against the final state.
                    return
                if board is None:
                    # One snapshot for the whole run: they all describe the same
                    # moment, and capturing per assertion would walk every card
                    # in the world once per `Then`.
                    board = Capture(decision.world)
                self.results.append(Evaluate(board, beat))
            self.index += 1

    def CheckNotOffered(self, beat: NotOfferedStep, decision: Any) -> None:
        """Check that the live decision omits one option on one known card."""
        world = decision.world
        if world is None:
            self.Record(beat, False,
                        "no board to check the option against", unresolvable=True)
            return

        try:
            card = ResolveCard(world, beat.card)
        except CardRefError as exc:
            self.Record(beat, False, str(exc), unresolvable=True)
            return

        wanted = NormaliseLabel(beat.option)
        object_id = int(card.object_id)
        for option in decision.selectable_options:
            if NormaliseLabel(option.name) != wanted:
                continue
            if int(option.bind_id) == object_id:
                self.Record(
                    beat, False,
                    f"the engine offered {DescribeOption(world, option)}")
                return
        self.Record(beat, True)

    def CheckCannot(self, beat: CannotStep, decision: Any) -> None:
        """Check that no offered option performs `beat.option` on `beat.card`.

        Passes two ways, because the engine expresses the same restriction two
        ways. Guard and stun both leave the option in place and empty its legal
        targets -- a stunned hero is still offered `Attack`, it just has nothing
        it may attack. An alter-ego is offered no `Attack` at all. Both are "I
        cannot attack Rhino", so both satisfy the claim.

        A card the scenario cannot resolve fails as unresolvable rather than
        passing. "You cannot attack a card that is not in this game" is true and
        worthless, and a spec that quietly rests on it is the silent-wrong-pass
        this harness exists to refuse.
        """
        world = decision.world
        if world is None:
            self.Record(beat, False,
                        "no board to check the restriction against", unresolvable=True)
            return

        try:
            card = ResolveCard(world, beat.card)
        except CardRefError as exc:
            self.Record(beat, False, str(exc), unresolvable=True)
            return

        wanted = NormaliseLabel(beat.option)
        object_id = int(card.object_id)
        for option in decision.selectable_options:
            if NormaliseLabel(option.name) != wanted:
                continue
            if object_id in [int(x) for x in option.all_legal_targets]:
                listing = ", ".join(self.LabelTargets(world, option)) or "nothing"
                self.Record(beat, False,
                            f"{beat.card} is a legal target for {option.name}; "
                            f"the engine would allow it alongside {listing}")
                return
        self.Record(beat, True)

    def CheckTargets(self, beat: TargetsStep, decision: Any) -> None:
        """Check which cards an offered option will accept.

        MARVEL-94. The positive counterpart to `CheckCannot`, and the thing that
        makes a one-option prompt assertable: `Futurist` with three legal
        targets is what "look at the top 3 cards of your deck" looks like from
        the engine's side, and the prompt table alone says nothing about them.

        An option the engine is not offering fails as unresolvable rather than
        vacuously: "the legal targets for an option that was never offered" has
        no truth value worth recording, and passing it would be the
        silent-wrong-pass this harness exists to refuse.
        """
        world = decision.world
        wanted = NormaliseLabel(beat.option)
        for option in decision.selectable_options:
            if NormaliseLabel(option.name) != wanted:
                continue
            # Compared on the card's *name*, the way an author writes it. The
            # descriptive form -- "Repulsor Blast (01031) in PlayerDeck" -- is
            # what the failure message prints, because a target list that
            # disagrees is much easier to read with the zone attached.
            actual = sorted(NormaliseLabel(name)
                            for name in self.TargetNames(world, option))
            expected = sorted(NormaliseLabel(card) for card in beat.targets)
            if actual == expected:
                self.Record(beat, True)
                return
            missing = [t for t in expected if t not in actual]
            extra = [t for t in actual if t not in expected]
            parts: List[str] = []
            if missing:
                parts.append(f"missing {', '.join(repr(t) for t in missing)}")
            if extra:
                parts.append(f"unexpected {', '.join(repr(t) for t in extra)}")
            listing = ", ".join(self.LabelTargets(world, option)) or "nothing"
            self.Record(beat, False,
                        f"{option.name} accepts {listing} ({'; '.join(parts)})")
            return
        self.Record(beat, False,
                    f"the engine is not offering {beat.option!r}; it offers "
                    f"{self.Offered(decision)}", unresolvable=True)

    def CheckLimit(self, beat: LimitStep, decision: Any) -> None:
        """Check the most targets an offered option will take.

        The other half of "up to N", and the half `TargetsStep` cannot reach.
        `the legal targets for "Play" are` pins the candidates and a `When`
        naming three of them pins that three is allowed; the ceiling is what
        says a fourth is not, and offering one only produces a refusal --
        correct engine behaviour with no passing spelling.

        `target_num_range[1]` is the **effective** ceiling:
        `Selector.GetTargetRange` clamps the printed maximum to the number of
        legal targets, so a board with three candidates answers 3 whether the
        card prints 3 or has no maximum at all. That is why the failure message
        says when the ceiling it found is the candidate count -- a scenario that
        hits it has not built a board that can see the printed number.
        """
        options, error = self.TargetCountOptions(beat, decision)
        if error:
            self.Record(beat, False, error, unresolvable=True)
            return
        option = options[0]
        low = int(option.target_num_range[0])
        high = int(option.target_num_range[1])
        if high == beat.maximum:
            self.Record(beat, True)
            return
        listing = ", ".join(self.LabelTargets(decision.world, option)) or "nothing"
        detail = (f"{option.name} takes {low}..{high} target(s) "
                  f"from {listing}")
        if high == len(option.all_legal_targets) and high < beat.maximum:
            detail += ("; that is the number of legal targets, so the board "
                       "has no more candidates than the ceiling and cannot "
                       "show what the ceiling is")
        self.Record(beat, False, detail)

    def CheckMinimum(self, beat: MinimumStep, decision: Any) -> None:
        """Check the fewest targets an offered option will take.

        The client receives only the effective range. In particular,
        `range="All"` becomes `(candidate count, candidate count)`, and a
        dynamic maximum below a raw minimum clamps both ends to that maximum.
        Reading the live tuple is therefore the only contract the browser and
        future C# runner can share without reaching into Python card scripts.
        """
        options, error = self.TargetCountOptions(beat, decision)
        if error:
            self.Record(beat, False, error, unresolvable=True)
            return
        option = options[0]
        low = int(option.target_num_range[0])
        high = int(option.target_num_range[1])
        if low == beat.minimum:
            self.Record(beat, True)
            return
        listing = ", ".join(self.LabelTargets(decision.world, option)) or "nothing"
        self.Record(
            beat, False,
            f"{option.name} takes {low}..{high} target(s) from {listing}")

    def TargetCountOptions(self, beat: Any, decision: Any) -> Tuple[List[Any], str]:
        """Resolve one option for a range assertion without trusting ordering."""
        wanted = NormaliseLabel(beat.option)
        options = [option for option in decision.selectable_options
                   if NormaliseLabel(option.name) == wanted]

        if beat.card:
            if decision.world is None:
                return [], f"there is no board to resolve {beat.card!r} against"
            try:
                card = ResolveCard(decision.world, beat.card)
            except CardRefError as exc:
                return [], str(exc)
            options = [option for option in options
                       if int(option.bind_id) == int(card.object_id)]

        subject = (f"{beat.option!r} on {beat.card!r}" if beat.card
                   else repr(beat.option))
        if not options:
            return [], (f"the engine is not offering {subject}; it offers "
                        f"{self.Offered(decision)}")
        if len(options) > 1:
            return [], (f"{subject} matches {len(options)} offered options; "
                        f"bind the assertion to a card")
        return options, ""

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
                # Name the options. Every other refusal in this class says what
                # the engine offered, and this one did not: it reported that a
                # choice was forced without saying which, so the author's only
                # way forward was to instrument the harness. A message that
                # cannot be acted on is the same cost as no message.
                self.Fail(
                    f"the transcript says {beat.Describe()!r}, but the engine is "
                    f"forcing a choice at '{decision.event_name}': it offers "
                    f"{self.Offered(decision)} and will not take a pass", decision)
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

        if beat.payment is not None and decision.world is not None:
            effect = decision.world.object_manager.effect_dict.get(int(command.id))
            if effect is None:
                self.Fail(
                    f"cannot verify the realized payment for {beat.Describe()}: "
                    f"effect {command.id} is not in the world", decision)
                return self.Halt(decision)
            self.pending_exact_payments.append(
                (effect, beat.payment, beat.Describe()))

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
            resources = BotCommand.BuildPayment(option, targets, beat.payment)
            if resources is None:
                if beat.payment is None:
                    reasons.append(
                        f"{option.name} is offered but cannot be paid for")
                else:
                    reasons.append(
                        f"{option.name} is offered but cannot be paid with "
                        f"exactly {beat.payment} resources")
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

    def TargetNames(self, world: Any, option: Any) -> List[str]:
        """The bare card name of each legal target, for comparison."""
        from tools.spec.resolve import CardIndex
        if world is None:
            return []
        index = CardIndex(world)
        names: List[str] = []
        for target_id in option.all_legal_targets:
            card = index.get(int(target_id))
            names.append(str(card.face.name) if card is not None else str(target_id))
        return names

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
        self.SettleExactPayments()

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
            elif isinstance(beat, (NotOfferedStep, CannotStep, MinimumStep,
                                   LimitStep)):
                # A restriction is a claim about a decision, so with no decision
                # left there is nothing to check. It does not pass by default:
                # "the engine never offered the chance" and "the engine offered
                # it and refused the target" are different findings.
                self.Record(beat, False,
                            "the game ended before there was a decision to "
                            "check this restriction against", unresolvable=True)
            else:
                self.Record(beat, False,
                            "the game ended before this step could be played")

    def SettleExactPayments(self, decision: Any = None) -> None:
        """Check completed payments before their reusable Effect is invoked again."""
        # The payment descriptor states what each generator *can* produce.
        # Some resource abilities ask a nested question and produce less than
        # they advertised (SP//dr Suit is the shipped example). The command can
        # only name generator ids, so exactness has to be checked against the
        # selected effect after `SpendResource` records what was really made.
        # `paid_this_res_effects` is cleared when the outer effect finishes; if
        # it is still populated, another transcript failure halted resolution
        # early and that earlier failure remains the useful diagnosis.
        if self.failure:
            return

        still_resolving: List[Tuple[Any, int, str]] = []
        for effect, expected, description in self.pending_exact_payments:
            if effect.context.paid_this_res_effects:
                still_resolving.append((effect, expected, description))
                continue

            actual = effect.context.paid_this_resources.val
            if actual != expected:
                message = (
                    f"{description} required exactly {expected} resources, "
                    f"but the selected payment effects generated {actual}")
                if decision is None:
                    self.failure = message
                else:
                    self.Fail(message, decision)
                break

        self.pending_exact_payments = still_resolving

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

    def Record(self, beat: Beat, passed: bool, message: str = "",
               *, unresolvable: bool = False) -> None:
        self.results.append(AssertionResult(
            step=ThenStep("transcript", "beat", beat.Describe()),
            passed=passed,
            message=message,
            unresolvable=unresolvable,
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
