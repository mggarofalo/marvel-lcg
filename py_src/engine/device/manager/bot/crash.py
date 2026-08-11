"""Turn a self-play failure into something you can replay.

Self-play trips assertions. The engine is dense with `assert`, and it
deliberately absorbs what they raise: `EffectInvoker`, `Message2.Send`, the cost
and target checkers and `Engine.EngineRun` all catch broadly so one broken card
cannot end the game, and all report through `Log.OnCrash` -- which re-raises
only when `Build.release` is false, and `build.py` hardcodes it true. So a card
script that blows up mid-game prints a traceback, `Engine.SaveCrash()` writes
`./crash.json` *once per process*, and the game carries on and saves as if
nothing happened.

That is the right behaviour for play and useless for generation: a corpus run
produces a wall of tracebacks with no seed, no step index, no digest and no
replayable scene. This module turns each one into an artefact that stands on its
own -- the scene (seed plus every input up to the failure), the step index, the
exception and its traceback, and the last state digest -- and deduplicates them
so one recurring bug produces one file rather than ten thousand.

Nothing here fixes what it finds. A bug that exists in the reference engine is a
decision to make later. See MARVEL-12.

Classification
--------------

Five kinds, resolved in this order:

    fabricated-input      an input in the recorded list was never made by the
                          policy: `FabricatedInputError`, and the runner's
                          refusals for a non-zero resolved timeout or a
                          fabricated input counted during the game
    invariant-violation   the state the engine computed is wrong, but every
                          input that produced it is genuine: the MARVEL-11
                          checker's `InvariantViolation`, replay verification
                          disagreeing, a scene that would not save
    engine-assert         `AssertionError` -- an `assert` in engine code fired
    timeout-stall         the game stopped making progress: `BotStuck`,
                          `NoProgressError`, `bot_max_steps`, `SetGameOver`
                          retry exhaustion
    unhandled-exception   everything else

The first two were one class until MARVEL-66, and the split is the whole reason
this classification is worth anything: **it decides whether the scene is
written**. `FabricatedInputError` is a timeout, and it is deliberately not a
stall, because what matters when triaging it is that the run has already
produced something untrustworthy.

`NoProgressError` is an `EngineIntegrityError` for propagation -- the handlers
between the bot and the runner catch broadly -- and not because anything is
corrupt. Its inputs are genuine and its scene is exactly what a reader wants,
so it belongs with the stalls, where nothing was corrupted and the game just
stopped.

Signatures
----------

Two failures share a signature when they came off the same code path. The
signature hashes the exception type and every frame as
`<path>:<function>:<lineno>`; frame paths are relative to the run directory and
slash-normalised so Windows and Linux agree on the same bug.

The exception *message* is excluded on purpose -- it carries card names and
object ids, which would split one bug into a signature per game. Line numbers
are included, so editing the code above a raise site produces a new signature
rather than silently merging two different bugs into one.
"""

import hashlib
import os
import traceback

from core import *
from core.errors import EngineIntegrityError
from core.utility.func import ROOT_DIR

CATEGORY_NAME = "BOT"

FAILURE_CLASS = Literal[
    "engine-assert",
    "fabricated-input",
    "invariant-violation",
    "timeout-stall",
    "unhandled-exception",
]

# Every class the report can contain, in the order a summary lists them:
# fabricated input first because it invalidates the artefacts themselves, then
# integrity, stalls last because they usually only cost a game.
FAILURE_CLASSES: Tuple[FAILURE_CLASS, ...] = (
    "fabricated-input",
    "invariant-violation",
    "engine-assert",
    "unhandled-exception",
    "timeout-stall",
)

# Marker set on an exception once it has been captured. The same exception can
# reach the collector twice -- `Log.OnCrash` observes it on the way through and
# `BotRunner` catches it if it keeps propagating -- and counting it twice would
# overstate how often the bug fires. The exception object is the identity;
# nothing else about the two sightings distinguishes them.
CAPTURED_ATTR = "_bot_crash_captured"

SIGNATURE_LENGTH = 8

# A fabricated input means a decision in the recorded list was never made by
# the policy -- it was returned by a timed-out wait. `Log.OnCrash` re-raises
# those *before* `Engine.SaveCrash` for exactly that reason: writing the scene
# would put the state we are refusing to keep on disk, where the next reader
# sees a replay rather than a refusal. So this class gets a sidecar and no
# scene. Nothing is lost -- the bot is deterministic, so the recorded seed and
# command regenerate the game. See MARVEL-32.
#
# **Only this class.** Until MARVEL-66 the rule covered every
# `EngineIntegrityError`, which swept in the MARVEL-11 checker: there every
# recorded input *was* made by the policy and only the state computed from them
# is wrong, so the scene replays faithfully -- `tools/invariants/probe_repro.py`
# asserts precisely that. Withholding it produced two artefacts that disagreed,
# one of them telling the reader to re-run a whole game from a seed when a
# step-indexed repro was sitting in the next directory.
SCENE_WITHHELD_CLASSES: Tuple[FAILURE_CLASS, ...] = ("fabricated-input",)
SCENE_WITHHELD_REASON = (
    "withheld: an input in this game was recorded from a timed-out wait rather "
    "than made by the policy, so writing the scene would look like a replay of "
    "a decision nobody made. Reproduce from the seed with `repro.command`. "
    "See MARVEL-32.")

################################################################################
#
def RelativeFrame(file_path: str) -> str:
    """A frame's file, as something two machines can agree on.

    `ROOT_DIR` is the working directory the engine was started from, which
    AGENTS.md requires to be `py_src/`. Anything under it becomes a relative
    slash-separated path. Anything outside it -- the standard library, a
    site-packages dependency -- becomes its bare filename, because its absolute
    path encodes an interpreter install that differs on every machine.
    """
    try:
        relative = os.path.relpath(file_path, ROOT_DIR)
    except ValueError:
        # Different drive on Windows; there is no relative path to compute.
        return os.path.basename(file_path)

    if relative.startswith(os.pardir):
        return os.path.basename(file_path)

    # Normalise this host's separators only. Replacing backslashes outright
    # would corrupt a POSIX filename that legitimately contains one.
    normalised = relative.replace(os.sep, "/")
    if os.altsep:
        normalised = normalised.replace(os.altsep, "/")
    return normalised

def FrameKeys(exc: BaseException) -> List[str]:
    """`<path>:<function>:<lineno>` for every frame, outermost first."""
    return [
        f"{RelativeFrame(frame.filename)}:{frame.name}:{frame.lineno}"
        for frame in traceback.extract_tb(exc.__traceback__)
    ]

def FormatTraceback(exc: BaseException, depth: int=5) -> str:
    """`traceback.format_exception`, with the machine taken back out.

    The standard formatter prints absolute filenames, which puts the author's
    home directory in every artefact and makes two reports of the same failure
    look different on two machines. Rebuild the frames through
    `RelativeFrame` instead; the source lines are unchanged.

    Chained exceptions are followed, because a card script that raises inside
    an `except` is exactly the shape where the first exception is the finding.
    """
    parts: List[str] = []
    chain: List[BaseException] = []

    current: 'BaseException|None' = exc
    while current != None and len(chain) < depth:
        chain.append(current)
        if current.__cause__ != None:
            following = "\nThe above exception was the direct cause of the following exception:\n\n"
            current = current.__cause__
        elif current.__context__ != None and not current.__suppress_context__:
            following = "\nDuring handling of the above exception, another exception occurred:\n\n"
            current = current.__context__
        else:
            break
        parts.append(following)

    # `chain` runs newest first; a traceback reads oldest first, and `parts`
    # holds the joiner that belongs *after* each older entry.
    rendered: List[str] = []
    for index, link in enumerate(reversed(chain)):
        rendered.append(FormatOne(link))
        joiner_index = len(chain) - 2 - index
        if 0 <= joiner_index < len(parts):
            rendered.append(parts[joiner_index])
    return "".join(rendered)

def FormatOne(exc: BaseException) -> str:
    frames = traceback.extract_tb(exc.__traceback__)
    lines: List[str] = []
    if frames:
        summary = traceback.StackSummary.from_list([
            (RelativeFrame(frame.filename), frame.lineno or 0, frame.name, frame.line)
            for frame in frames
        ])
        lines.append("Traceback (most recent call last):\n")
        lines.extend(summary.format())
    lines.extend(traceback.format_exception_only(type(exc), exc))
    return "".join(lines)

def HashSignature(*parts: str) -> str:
    payload = "\n".join(parts).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()[:SIGNATURE_LENGTH]

def Classify(exc: BaseException) -> FAILURE_CLASS:
    """Which bucket an exception belongs in. Order matters -- see the docstring.

    The three `EngineIntegrityError` subclasses land in three different buckets,
    because the base class is about *propagation* -- surviving the broad handlers
    between the raise and the runner -- and says nothing about whether the
    recorded inputs can be trusted, which is what decides the artefact.
    """
    from engine.device.manager.bot.policy import BotStuck
    from engine.device.manager.bot.progress import NoProgressError
    from engine.device.manager.base import FabricatedInputError

    if isinstance(exc, FabricatedInputError):
        # Checked before the base class: this is the one that corrupts the
        # recorded inputs, and the only one whose scene is withheld.
        return "fabricated-input"
    if isinstance(exc, NoProgressError):
        # Genuine inputs, uncorrupted state, a game that stopped advancing.
        return "timeout-stall"
    if isinstance(exc, EngineIntegrityError):
        return "invariant-violation"
    if isinstance(exc, AssertionError):
        return "engine-assert"
    if isinstance(exc, BotStuck):
        return "timeout-stall"
    return "unhandled-exception"

################################################################################
#
@dataclass(frozen=True)
class Failure:
    """What went wrong, described independently of where it was seen.

    The same `Failure` describes every occurrence of one bug; `Occurrence`
    carries the part that differs between sightings.
    """
    kind            : FAILURE_CLASS
    signature       : str
    title           : str
    exception_type  : str = ""
    message         : str = ""
    traceback_text  : str = ""
    # Set for failures the runner detects without an exception, so the report
    # names the check that refused rather than only its prose.
    reason_key      : str = ""

    @staticmethod
    def FromException(exc: BaseException) -> 'Failure':
        frames = FrameKeys(exc)
        exception_type = type(exc).__name__
        kind = Classify(exc)

        if frames:
            # The innermost frame is where it actually went wrong.
            site = frames[-1]
            where = ":".join(site.split(":")[:2])
        else:
            # No traceback: the exception was constructed but never raised, or
            # it was re-raised after the traceback was cleared.
            where = "<no traceback>"

        return Failure(
            kind            = kind,
            signature       = HashSignature(exception_type, *frames),
            title           = f"{exception_type} at {where}",
            exception_type  = exception_type,
            message         = str(exc),
            traceback_text  = FormatTraceback(exc),
        )

    @staticmethod
    def FromReason(kind: FAILURE_CLASS, reason_key: str, detail: str) -> 'Failure':
        """A failure the runner detected itself, with no exception to hash.

        `reason_key` identifies the check that refused and is the whole
        signature; `detail` is prose and carries per-game numbers, so it stays
        out of it for the same reason an exception message does.
        """
        return Failure(
            kind        = kind,
            signature   = HashSignature(kind, reason_key),
            title       = detail or reason_key,
            message     = detail,
            reason_key  = reason_key,
        )

@dataclass(frozen=True)
class Occurrence:
    """One sighting of a failure: which game, and how far in."""
    seed        : int
    step        : int
    decisions   : int = 0
    digest      : str = ""

@dataclass
class CrashGroup:
    """Every sighting of one signature, plus the artefact that reproduces it."""
    failure     : 'Failure'
    occurrences : int = 0
    seeds       : List[int] = field(default_factory=lambda: [])
    # The occurrence that reached this failure in the fewest steps, and the
    # scene saved from it. Fewest steps is the minimal repro: nothing can be
    # dropped from a replay without changing the game, so the shortest input
    # list that still reaches the bug is as small as a repro gets.
    minimal     : 'Occurrence|None' = None
    scene_file  : str = ""
    # Why there is no scene, when there is none. Empty when one was written.
    scene_note  : str = ""

    @property
    def games(self) -> int:
        return len(self.seeds)

    def IsShorterThanMinimal(self, occurrence: 'Occurrence') -> bool:
        return self.minimal == None or occurrence.step < self.minimal.step

################################################################################
#
class CrashCollector:
    """Collects failures during a run and writes the artefacts at the end of it.

    Only one thing here touches the world: `save_scene`, which is injected so
    the collection rules can be exercised without an engine.
    """

    def __init__(self, save_scene: 'Callable[[str], str|None]|None'=None,
                 max_signatures: int=200) -> None:
        # `save_scene(path)` writes the live scene and returns where it landed,
        # or None if it declined to.
        self.save_scene = save_scene
        self.max_signatures = max_signatures

        self.groups: Dict[str, CrashGroup] = {}
        # Failure events seen, including the ones past `max_signatures` that
        # got no group of their own.
        self.captured = 0
        # The signatures that got no group, held as a set: one bug that fires
        # ten thousand times past the cap is one signature the report is not
        # showing, not ten thousand.
        self.dropped: 'Set[str]' = set()

        self.seed = -1

        # A crash reporter must never become the crash. `Log.OnCrash` calls the
        # observer from inside an exception handler, and saving a scene runs
        # engine code that can raise into another handler.
        self.in_capture = False

    ################################################################################
    #
    def BeginGame(self, seed: int) -> None:
        self.seed = seed

    def Capture(self, failure: 'Failure', occurrence: 'Occurrence') -> bool:
        """Record one sighting. Returns whether it was counted.

        A sighting that arrives while another is being recorded is dropped
        entirely -- saving a scene runs engine code that can raise into another
        handler, and that secondary failure is an artefact of reporting the
        first one, not a finding.
        """
        if self.in_capture:
            return False

        self.in_capture = True
        try:
            return self.Record(failure, occurrence)
        finally:
            self.in_capture = False

    def CaptureException(self, exc: BaseException, occurrence: 'Occurrence') -> bool:
        """`Capture` for an exception, counting each exception object once.

        The same exception can arrive twice -- `Log.OnCrash` observes it on the
        way through and `BotRunner` catches it if it keeps propagating -- and
        the two sightings are one failure.
        """
        if getattr(exc, CAPTURED_ATTR, False):
            return False

        counted = self.Capture(Failure.FromException(exc), occurrence)
        if counted:
            try:
                setattr(exc, CAPTURED_ATTR, True)
            except AttributeError:
                # A `BaseException` subclass with `__slots__` and no `__dict__`.
                # It will be counted again if it is seen again; that is a worse
                # count, not a broken run.
                pass
        return counted

    def Record(self, failure: 'Failure', occurrence: 'Occurrence') -> bool:
        self.captured += 1

        group = self.groups.get(failure.signature)
        if group == None:
            if len(self.groups) >= self.max_signatures:
                # Counted above but given no group, so the report can say how
                # much it is not showing rather than looking complete. Still a
                # recorded sighting: the caller must not offer it again.
                self.dropped.add(failure.signature)
                return True
            group = CrashGroup(failure=failure)
            self.groups[failure.signature] = group

        group.occurrences += 1
        if occurrence.seed not in group.seeds:
            group.seeds.append(occurrence.seed)

        if group.IsShorterThanMinimal(occurrence):
            group.minimal = occurrence
            self.SaveSceneFor(group)

        return True

    def SaveSceneFor(self, group: 'CrashGroup') -> None:
        """Write the scene that reproduces `group`, replacing any earlier one.

        The name is derived from the signature, so a shorter repro for a failure
        already seen overwrites its own file instead of adding another.
        """
        group.scene_file = ""

        if group.failure.kind in SCENE_WITHHELD_CLASSES:
            group.scene_note = SCENE_WITHHELD_REASON
            return

        if self.save_scene == None:
            group.scene_note = "scene saving is not configured for this run"
            return

        try:
            saved = self.save_scene(SceneName(group.failure))
        except Exception as exc:
            # The scene is the best artefact, not the only one: the sidecar
            # still carries the seed, the step, the digest and the traceback,
            # and the repro command regenerates the game. Losing the scene must
            # not lose the finding as well.
            group.scene_note = f"{type(exc).__name__}: {exc}"
            return

        if saved:
            group.scene_file = os.path.basename(saved)
            group.scene_note = ""
        else:
            group.scene_note = "the engine declined to save the scene"

    ################################################################################
    #
    @property
    def has_failures(self) -> bool:
        return self.captured > 0

    def Groups(self) -> List['CrashGroup']:
        """Groups in report order: most frequent first, signature to break ties."""
        return sorted(self.groups.values(),
                      key=lambda group: (-group.occurrences, group.failure.signature))

    def Summary(self) -> Dict[str, Any]:
        """The counts a corpus manifest wants, without the tracebacks."""
        by_class = {
            kind: sum(group.occurrences for group in self.groups.values()
                      if group.failure.kind == kind)
            for kind in FAILURE_CLASSES
        }
        recorded = sum(group.occurrences for group in self.groups.values())
        return {
            "captured": self.captured,
            "signatures": len(self.groups),
            "by_class": {kind: count for kind, count in by_class.items() if count},
            # True when `max_signatures` stopped new signatures being recorded.
            # A report that silently drops findings reads as a clean run.
            "truncated": len(self.dropped) > 0,
            # Distinct bugs the report is not showing, and what they cost in
            # sightings. Both, because either alone reads as the other: "12
            # dropped" is very different news if it is 12 bugs or 12 hits on one.
            "dropped_signatures": len(self.dropped),
            "dropped_occurrences": self.captured - recorded,
        }

################################################################################
#
def SceneName(failure: 'Failure') -> str:
    return f"bot-crash-{failure.kind}-{failure.signature}.json"

def SidecarName(failure: 'Failure') -> str:
    return f"bot-crash-{failure.kind}-{failure.signature}.crash.json"

def DigestHash(digest: str) -> str:
    """A fingerprint of a state digest, for saying "same state" in one line.

    The v2 digest is a whole document -- every card, zone and named field -- so
    it is the right thing to diff against and the wrong thing to put in a
    summary. See `docs/state-digest-v2.md`.
    """
    if not digest:
        return ""
    return hashlib.sha256(digest.encode("utf-8")).hexdigest()[:16]

def BuildRepro(group: 'CrashGroup', context: Dict[str, Any],
               *, with_digest: bool=False) -> Dict[str, Any]:
    """Everything needed to get this failure back, from this entry alone.

    `with_digest` carries the whole state digest as well as its fingerprint.
    The sidecar wants it -- it is what `digest.Diff` compares a re-run against.
    The run report does not: one board dump per signature would bury the thing
    the report exists to show.
    """
    minimal = group.minimal
    seed = minimal.seed if minimal else -1
    digest = minimal.digest if minimal else ""

    repro: Dict[str, Any] = {
        "seed": seed,
        "step": minimal.step if minimal else -1,
        "decisions": minimal.decisions if minimal else 0,
        "digest_hash": DigestHash(digest),
        "scene": group.scene_file,
        "scene_note": group.scene_note,
        "command": ReproCommand(context, seed),
    }
    if with_digest:
        repro["digest"] = digest
    return repro

def ReproCommand(context: Dict[str, Any], seed: int) -> str:
    """The invocation that replays the game this failure came out of."""
    heroes = " ".join(context.get("heroes") or [])
    parts = [
        "python main.py -bot",
        f"-bot_scenario {context.get('scenario', '')}",
        f"-bot_heroes {heroes}",
        f"-bot_seed {seed}",
        "-bot_games 1",
    ]
    policy = context.get("policy")
    if policy:
        parts.append(f"-bot_policy {policy}")
    encounter_sets = context.get("encounter_sets") or []
    if encounter_sets:
        parts.append(f"-bot_encounter_sets {' '.join(encounter_sets)}")
    rules = context.get("rules") or []
    if rules:
        parts.append(f"-bot_rules {' '.join(rules)}")
    return " ".join(parts)

def BuildEntry(group: 'CrashGroup', context: Dict[str, Any],
               *, with_digest: bool=False) -> Dict[str, Any]:
    """One failure, in full. This is what a sidecar file contains."""
    failure = group.failure
    entry: Dict[str, Any] = dict(context)
    entry.update({
        "signature": failure.signature,
        "class": failure.kind,
        "title": failure.title,
        "exception": failure.exception_type,
        "reason": failure.reason_key,
        "message": failure.message,
        "occurrences": group.occurrences,
        "games": group.games,
        "seeds": list(group.seeds),
        "repro": BuildRepro(group, context, with_digest=with_digest),
        "traceback": failure.traceback_text,
    })
    return entry

def BuildSidecar(group: 'CrashGroup', context: Dict[str, Any]) -> Dict[str, Any]:
    """One failure as a file that stands on its own, digest and all."""
    return BuildEntry(group, context, with_digest=True)

def BuildReport(collector: 'CrashCollector', context: Dict[str, Any]) -> Dict[str, Any]:
    """The per-run summary: distinct signatures, counts, and a repro for each.

    Reads no clock and no host, so it is as reproducible as the scenes it sits
    beside (MARVEL-27).
    """
    report: Dict[str, Any] = {"generator": "bot"}
    report.update(context)
    report.update(collector.Summary())
    report["failures"] = [BuildEntry(group, context) for group in collector.Groups()]
    return report

def FormatSummary(collector: 'CrashCollector') -> List[str]:
    """The end-of-run summary, one line per distinct signature."""
    summary = collector.Summary()
    lines = [
        f"{summary['captured']} failure(s), "
        f"{summary['signatures']} distinct signature(s)"
    ]
    if summary["truncated"]:
        lines.append(
            f"{summary['dropped_signatures']} further signature(s) "
            f"({summary['dropped_occurrences']} occurrence(s)) were counted but "
            f"not recorded (bot_max_crash_signatures={collector.max_signatures})")

    for group in collector.Groups():
        minimal = group.minimal
        where = f"seed {minimal.seed} step {minimal.step}" if minimal else "unknown"
        lines.append(
            f"  {group.failure.signature}  {group.failure.kind:<20} "
            f"x{group.occurrences} in {group.games} game(s)  [{where}]  "
            f"{group.failure.title}")
    return lines
