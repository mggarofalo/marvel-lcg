"""Where two internal `Temp0` cleanups meet in one forced-order prompt.

`AbilityType.Temp0` and `Temp0UI` map to `TimingPriority.Rule`
(`game/ability/ability_type.py`) and are forced, so two of them arriving on one
message land in one batch -- and `EventManager.ProcessForcedEffect` used to ask
the first player to sequence them. The options had no names (`Temp #1`,
`Temp #2`) because there is nothing to name. That is MARVEL-95, and it is
MARVEL-91's defect (`TimingPriority.Constant`) one priority band over.

**The pair does not come from where the issue expected.** MARVEL-95 predicted a
card carrying two constants, each contributing one cleanup through
`AbilityFactory.WhileValid` / `WhileThisStateUpdate` and
`RegisterTemp(..., unregister_after_exec=True)`. Those cleanups cannot reach the
prompt at all: `FaceEffect.RegisterTemp` files every ability under
`global_effects` whatever `Ability.is_local` says, and
`EventManager.GetEffectCategory` sends anything with `flags.is_temp` to the
`"Rule"` category, which `ProcessRuleEffect` resolves with no prompt. The batch
that reaches `ProcessForcedEffect` is built from `local_effect_priority_forced`
-- *local* effects only.

The cleanups that are local come from `environment_helper2.apply_environment_internal`,
and they are registered **onto the face being modified**, one per continuous
modifier. So the population is not cards carrying two constants; it is faces
sitting under two continuous modifiers at once. It cannot be enumerated one card
at a time, which is why the scan below measures what a card contributes to
*other* faces as well as what it carries.

Static identity is not evidence -- MARVEL-93 is the cautionary case, where an
identical static shape one band over turned out to be two mutually exclusive
modes of one card and no defect at all. So this probe reports four things
separately:

  selfplay  Play the determinism matrix and write down every multi-candidate
            forced batch with its ability types and the source line each one was
            built at. "A prompt fired" and "a prompt fired on two internal
            cleanups" are different claims.

  scan      **The enumeration rule.** Put every card the engine can generate
            into play, alone, on a bare puzzle board plus a fixed roster of
            vanilla targets, and count the local, forced `Temp0`/`Temp0UI`
            effects that `AfterCardLeavePlay` would trigger on *every* face in
            the world. Two on one face means this card reaches the prompt by
            itself; one on somebody else's face makes it an **applier**, able to
            pair with another.

  drive     Build each single-card board and every pair of appliers, then
            discard (or failing that remove) every face that ended up carrying
            two or more cleanups, recording the batches that reach
            `SelectForcedEffect`. A card that will not enter play, or a face
            that will not leave it, is reported with the reason -- "not reached,
            and here is why" is a result; "not tried" is not.

  digest    Replay every prompting board twice, with the first player forced
            onto candidate 0 and then onto candidate 1, and compare
            `World.CalculateDigest()`. The runs' chosen indices are compared
            too: identical digests prove nothing if both runs answered the same
            way. If the two orders ever disagree, the cleanups are not
            interchangeable and the prompt is real -- a stop-and-report finding,
            not a fix.

Exit code is 1 only if some order changed a digest. "No prompt was reached" is a
zero, because that is what this looks like once the fix is in.

Run:  python -m tools.determinism.probe_temp0_order
      python -m tools.determinism.probe_temp0_order --stage selfplay
      python -m tools.determinism.probe_temp0_order --cards 01136+01140
      python -m tools.determinism.probe_temp0_order --json out.json
"""

from __future__ import annotations

import argparse
import contextlib
import io
import json
import os
import subprocess
import sys
from typing import Any, Dict, List, Sequence

MARKER = "<<<TEMP0>>>"

SCENARIO = "rhino"
HERO = "spider_man"

# Cards put into play *after* the board under test, so that a continuous
# modifier has something to modify. Every one of them has no card script at all
# (`CardsDB.FindAbilities` returns nothing), so they contribute no cleanups of
# their own and cannot be mistaken for an applier -- the only exception is
# `ultron_facedown_drone`, which is here because it is the card the wide
# self-play matrix actually caught and because a 0-hit-point minion only
# survives entering play when something is already granting it health. Without
# it the drone's own case is unreachable from a bare board.
TARGETS = (
    "minion",                   # a generic vanilla minion
    "01101",                    # another, in case a modifier names a trait
    "01076",                    # a vanilla ally
    "01108",                    # a vanilla side scheme
    "ultron_facedown_drone",    # the DRONE minion from the self-play batch
)


################################################################################
# Engine

def Boot() -> None:
    from tools.spec.harness import EnsureEngine
    EnsureEngine()


def GeneratableCardIds() -> List[str]:
    """Every card id the engine can build a `Card` for, in file order.

    `CardsDB.papers` is the load of `data/cards.json`; `CardsDB.FindAbilities`
    is what turns one into abilities. A paper with no script still generates --
    a vanilla ally has no abilities and no cleanups -- so nothing is filtered
    here. The scan below is what decides which ones matter.
    """
    from cards.database import CardsDB
    return sorted(CardsDB.papers.keys())


class Silence:
    """The engine narrates every game to stdout; a scan of 3800 cards drowns in it."""

    def __init__(self) -> None:
        self.buffer = io.StringIO()

    def __enter__(self) -> io.StringIO:
        self._redirect = contextlib.redirect_stdout(self.buffer)
        self._redirect.__enter__()
        return self.buffer

    def __exit__(self, *args: Any) -> None:
        self._redirect.__exit__(*args)


################################################################################
# One puzzle world

def BuildScene() -> Any:
    """A puzzle scene: villain, main scheme, one identity, and nothing else.

    Same shape `tools/spec/harness.BuildPuzzleScene` builds -- no encounter deck,
    no player deck -- so the only cards on the board are the ones this probe puts
    there.
    """
    from tools.spec.harness import LoadHeroJson, LoadScenarioJson

    from engine.lib import Json
    from game.scene.scene import Scene

    scenario = LoadScenarioJson(SCENARIO)
    hero = LoadHeroJson(HERO)
    version = str(scenario.get("version", ""))

    data = {
        "version": version,
        "metadata": {"seed": 1, "comment": "probe_temp0_order",
                     "cover": "", "is_puzzle": True},
        "campaign": {
            "version": version,
            "name": scenario.get("name", SCENARIO),
            "villain": list(scenario.get("villain", [])),
            "expert": False,
            "schemes": list(scenario.get("schemes", [])),
            "set_aside": [], "encounters": [], "encounter_sets": [], "modular_sets": [],
        },
        "players": [{
            "version": version, "name": "Player 0",
            "hero": list(hero.get("hero", [])),
            "hero_deck": [], "obligations": [], "nemesis_set": [],
            "set_aside": [], "player_deck": [],
        }],
        "puzzle": [],
    }
    scene = Json.LoadsAs(Json.Dumps(data), Scene)
    scene.UpdateVersion()
    return scene


class ForcedOrderPolicy:
    """`FirstLegalPolicy`, except that the forced-order prompt is answered by index.

    The probe has to be able to say "the first player picked the second option"
    without reaching around the prompt, because the point of the digest check is
    that both answers travel the whole path. `AskForcedOrder` sets `in_prompt`
    for the duration of the ask, and this policy answers that one decision with
    `pick` and everything else the way self-play would.
    """

    def __init__(self, pick: int) -> None:
        from engine.device.manager.bot.policies import BotPolicyFactory
        self.inner = BotPolicyFactory.Create("first", 0)
        self.pick = pick
        self.in_prompt = False
        self.answered = 0

    name = "temp0-probe"

    def OnGameStart(self, seed: int) -> None:
        self.inner.OnGameStart(seed)

    def Choose(self, decision: Any) -> Any:
        from engine.device.manager.bot.command import BotCommand

        if self.in_prompt:
            commands = BotCommand.BuildAll(decision.selectable_options)
            if self.pick < len(commands):
                self.answered += 1
                return commands[self.pick]
        return self.inner.Choose(decision)


class Game1:
    """One puzzle game, set up and ready to have cards pushed through it."""

    def __init__(self, pick: int = 0) -> None:
        from engine import Engine
        from engine.device.manager.bot.manager import BotDeviceManager
        from game.game import Game
        from game.statistics.game_statistics import GameStatistics

        statistics = GameStatistics()
        statistics.Load()
        Engine.statistics = statistics

        self.policy = ForcedOrderPolicy(pick)
        self.policy.OnGameStart(0)
        device_manager = BotDeviceManager(self.policy)
        Engine.device_manager = device_manager

        self.game = Game(statistics, device_manager)
        Engine.game = self.game
        self.game.session.SetScene(BuildScene(), "Replay")
        self.game.controller_manager.skip.SetSkipTo(0)

        with Silence():
            self.ok = self.game.GameSetup()
        self.world = self.game.world

    def Puzzle(self) -> Any:
        from game.puzzle.puzzle import RunPuzzle
        return RunPuzzle(self.world)

    def AddTargets(self, puzzle: Any) -> List[str]:
        """Put the target roster into play. Call *after* the board under test.

        Order matters and is the whole reason this is a separate step: a
        continuous modifier granting health has to already be in play for a
        0-hit-point minion to survive entering it.
        """
        landed: List[str] = []
        for card_id in TARGETS:
            try:
                with Silence():
                    face = puzzle.CreateCard(card_id)
                    puzzle.PutIntoPlay(face)
                if face.card.IsOnField():
                    landed.append(card_id)
            except Exception:
                # A target that will not enter play is not a result about the
                # board under test; the roster is scaffolding.
                continue
        return landed

    def Digest(self) -> str:
        return self.world.render.CalculateDigest() if self.world else ""


################################################################################
# Instrumentation

class Watcher:
    """Records every forced batch that reaches the first-player tie-break.

    Wraps `SelectForcedEffect` rather than reimplementing its filter, so what is
    counted is what the engine actually offered. `AskForcedOrder` is wrapped too,
    to tell `ForcedOrderPolicy` when the decision it is about to answer is the
    ordering prompt and not some unrelated question the discard opened.
    """

    def __init__(self, policy: "ForcedOrderPolicy|None" = None) -> None:
        self.batches: List[Dict[str, Any]] = []
        self.policy = policy
        self._select = None
        self._ask = None

    def __enter__(self) -> "Watcher":
        from game.event.manager import EventManager

        self._select = EventManager.SelectForcedEffect
        self._ask = EventManager.AskForcedOrder
        select = self._select
        ask = self._ask
        watcher = self

        def Select(forced_effects, ask_first_player):
            candidates = [x for x in forced_effects
                          if not x.ability.flags.is_delay_ability]
            chosen = select(forced_effects, ask_first_player)
            if len(candidates) > 1:
                record = Describe(candidates)
                # **The control on the digest evidence.** "Both orders gave the
                # same digest" is vacuous unless the two runs answered
                # differently, so the index the first player actually landed on
                # is recorded and compared. See `DigestOne`.
                record["chosen"] = next(
                    (index for index, effect in enumerate(candidates)
                     if effect is chosen), -1)
                watcher.batches.append(record)
            return chosen

        def Ask(self_manager, first_player, candidates):
            if watcher.policy is not None:
                watcher.policy.in_prompt = True
            try:
                return ask(self_manager, first_player, candidates)
            finally:
                if watcher.policy is not None:
                    watcher.policy.in_prompt = False

        EventManager.SelectForcedEffect = staticmethod(Select)  # type: ignore[assignment]
        EventManager.AskForcedOrder = Ask  # type: ignore[assignment]
        return self

    def __exit__(self, *args: Any) -> None:
        from game.event.manager import EventManager
        EventManager.SelectForcedEffect = staticmethod(self._select)  # type: ignore[assignment]
        EventManager.AskForcedOrder = self._ask  # type: ignore[assignment]


def Describe(candidates: Sequence[Any]) -> Dict[str, Any]:
    """A batch, as a record. Never raises.

    This runs inside a live `SelectForcedEffect`, and the engine's message
    broadcast catches broadly (`game/message/message.py`) -- so an exception
    raised here would be swallowed, the batch would resolve differently, and the
    probe would report *fewer* batches than there were while silently changing
    the game it was measuring. That happened once during this work: reading a
    `card_id` attribute a `CardFace` does not have shortened the ultron game by
    one step and reported zero batches.
    """
    try:
        first = candidates[0]
        return {
            "priority": str(first.ability.priority),
            "types": [str(effect.ability.type) for effect in candidates],
            "names": [str(effect.this.name) for effect in candidates],
            "cards": sorted({effect.this.card.object_id for effect in candidates}),
            "card_ids": sorted({effect.this.paper.card_id for effect in candidates}),
            "local": [bool(effect.is_local) for effect in candidates],
            # Where each ability was built. `Ability.code_action` is
            # "<file>:<line>" of the operation it carries, which is the only
            # thing that says *which* registration produced a nameless Temp0.
            "where": [Where(effect.ability) for effect in candidates],
            "when": [str(effect.ability.when) for effect in candidates],
            "labels": Labels(candidates),
            "count": len(candidates),
        }
    except Exception as exc:
        return {"priority": "?", "types": [], "names": [], "cards": [],
                "card_ids": [], "local": [], "where": [], "when": [], "labels": [],
                "count": len(candidates), "error": f"{type(exc).__name__}: {exc}"}


def Where(ability: Any) -> str:
    """`<file>:<line>` of the operation an ability carries, relative to `py_src/`."""
    code = getattr(ability, "code_action", "")
    root = os.getcwd() + os.sep
    return str(code).replace(root, "").replace("\\", "/")


def Labels(candidates: Sequence[Any]) -> List[str]:
    from game.event.manager import EventManager
    try:
        return list(EventManager.ForcedOrderLabels(list(candidates)))
    except Exception as exc:  # the labels are diagnostic here, not load-bearing
        return [f"<{type(exc).__name__}>"]


TEMP0_TYPES = ("AbilityType.Temp0", "AbilityType.Temp0UI")


def AllTemp0(batch: Dict[str, Any]) -> bool:
    """Every candidate in the batch is an internal cleanup. That is MARVEL-95's shape."""
    return bool(batch["types"]) and all(name in TEMP0_TYPES for name in batch["types"])


################################################################################
# Scan: the enumeration rule
#
# The issue predicted that a card carrying two constants would carry two Temp0
# cleanups of its own. It does not, and measuring that was the first thing this
# probe did. Two facts move the population somewhere else entirely:
#
#   1. `FaceEffect.RegisterTemp` appends to `global_effects` regardless of
#      `Ability.is_local`, and `EventManager.GetEffectCategory` sends anything
#      with `flags.is_temp` to the `"Rule"` category, which `ProcessRuleEffect`
#      resolves with no prompt. So the cleanups that `WhileValid` and `WhenEvent`
#      register through `RegisterTemp` can never reach the tie-break at all.
#
#   2. The cleanups that *do* reach it are registered with `face.effect.Registers`
#      -- local -- and the face they are registered on is the face being
#      **modified**, not the card doing the modifying. `environment_helper2.py`
#      is where that happens: `apply_environment_internal` puts one local
#      `WhileThisStateUpdate(Temp0)` on every face a continuous modifier applies
#      to, so it can take the modifier back off when that face leaves play,
#      flips, is set as another card, or the villain advances.
#
# So the population is not "cards with two constants". It is **faces sitting
# under two continuous modifiers at once** -- one card being written to by two
# others. That is why it cannot be enumerated one card at a time, and why the
# scan below measures what a card *contributes to other faces* as well as what
# it carries.

def BatchMessages() -> Dict[str, Any]:
    """The messages a `WhileThisStateUpdate` cleanup listens on.

    `AbilityFactoryState.WhileThisStateUpdate` builds the union
    `AfterCardLeavePlay | WhenCardFlip | WhenCardSetAs | WhenVillainAdvance`, so
    the cleanups batch together on any of the four, not only on leaving play.
    Scanning all four is what stops "the prompt is unreachable" being an artefact
    of only ever having tried one route out.
    """
    from game.message import Message
    return {
        "AfterCardLeavePlay": Message.AfterCardLeavePlay,
        "WhenCardFlip": Message.WhenCardFlip,
        "WhenCardSetAs": Message.WhenCardSetAs,
        "WhenVillainAdvance": Message.WhenVillainAdvance,
    }


def CleanupsOn(face: Any, message_type: Any) -> List[Any]:
    """The local, forced `Temp0`/`Temp0UI` effects on one face that `message_type` triggers.

    This mirrors what the engine itself will assemble. `BroadcastMessage` builds
    the list it hands to `ProcessForcedEffect` from `local_effect_priority_forced`
    -- the *local* effects `FindLocalEffects` gathered, filtered to one
    `TimingPriority` and to `effect.is_forced` -- so nothing else can be in the
    batch, and everything here will be.
    """
    from game.ability.ability_type import AbilityType

    found: List[Any] = []
    for effect in face.effect.local_effects:
        ability = effect.ability
        if ability.type not in (AbilityType.Temp0, AbilityType.Temp0UI):
            continue
        if not effect.is_forced or effect.is_unregister:
            continue
        try:
            admits = issubclass(message_type, ability.when)
        except TypeError:
            continue
        if admits:
            found.append(effect)
    return found


def Inventory(world: Any, message_name: str = "AfterCardLeavePlay") -> Dict[int, int]:
    """Every card in the world -> how many cleanups its faces carry, right now."""
    message_type = BatchMessages()[message_name]
    counts: Dict[int, int] = {}
    for object_id, card in world.object_manager.card_dict.items():
        faces = [card.face] + list(card.back_faces)
        total = sum(len(CleanupsOn(face, message_type)) for face in faces)
        if total:
            counts[object_id] = total
    return counts


def CardLabel(world: Any, object_id: int) -> str:
    card = world.object_manager.card_dict.get(object_id)
    if card is None:
        return str(object_id)
    return f"{card.face.paper.card_id}({card.face.name})"


def ScanOne(card_id: str) -> Dict[str, Any]:
    """Put one card into play on a bare board and measure the cleanups it creates.

    Two numbers come out and they answer different questions:

      `carried`      the most cleanups on any single face afterwards. Two or more
                     means this card alone reaches the prompt.
      `contributed`  how many cleanups this card put onto faces other than a
                     face it already owned. One or more makes it an *applier*, a
                     card that can pair with another to reach two.
    """
    record: Dict[str, Any] = {
        "card_id": card_id, "carried": 0, "contributed": 0, "in_play": False,
        "targets": [], "note": "", "name": "",
    }
    try:
        game = Game1()
        if not game.ok or game.world is None:
            record["note"] = "the puzzle game did not set up"
            return record
        world = game.world
        puzzle = game.Puzzle()
        before = Inventory(world)
        with Silence():
            face = puzzle.CreateCard(card_id)
            record["name"] = face.name
            puzzle.PutIntoPlay(face)
        record["in_play"] = bool(face.card.IsOnField())
        if not record["in_play"]:
            record["note"] = "PutIntoPlay left the card off the board"
        record["targets_landed"] = game.AddTargets(puzzle)
        after = Inventory(world)

        own = face.card.object_id
        record["carried"] = max(after.values(), default=0)
        record["contributed"] = sum(
            count - before.get(object_id, 0)
            for object_id, count in after.items() if object_id != own)
        record["targets"] = sorted(
            CardLabel(world, object_id) for object_id, count in after.items()
            if object_id != own and count > before.get(object_id, 0))
        record["worst"] = sorted(
            (count, CardLabel(world, object_id))
            for object_id, count in after.items())[-3:]
    except Exception as exc:
        record["note"] = f"{type(exc).__name__}: {exc}"
    return record


################################################################################
# Drive

LEAVE_PLAY_ROUTES = ("Discard", "Remove")


def DriveBoard(card_ids: Sequence[str], pick: int = 0) -> Dict[str, Any]:
    """Put a board together, then take every doubly-modified face out of play.

    One or more cards go into play; then every card in the world carrying two or
    more cleanups is discarded (or, failing that, removed) and the batches that
    reach `SelectForcedEffect` are recorded. One card in `card_ids` reproduces
    the single-card case; two reproduces the applier pair.
    """
    key = "+".join(card_ids)
    record: Dict[str, Any] = {
        "card_id": key, "board": list(card_ids), "pick": pick, "in_play": [],
        "doubled": [], "left_play": [], "batches": [], "note": "", "digest": "",
        "name": "",
    }
    try:
        game = Game1(pick=pick)
        if not game.ok or game.world is None:
            record["note"] = "the puzzle game did not set up"
            return record
        world = game.world
        puzzle = game.Puzzle()

        with Watcher(game.policy) as watcher:
            names: List[str] = []
            for card_id in card_ids:
                with Silence():
                    face = puzzle.CreateCard(card_id)
                    puzzle.PutIntoPlay(face)
                names.append(face.name)
                if face.card.IsOnField():
                    record["in_play"].append(card_id)
            record["name"] = " + ".join(names)
            record["targets_landed"] = game.AddTargets(puzzle)
            if len(record["in_play"]) != len(card_ids):
                record["note"] = ("PutIntoPlay left "
                                  f"{len(card_ids) - len(record['in_play'])} of "
                                  f"{len(card_ids)} off the board")

            doubled = [object_id for object_id, count in Inventory(world).items()
                       if count >= 2]
            record["doubled"] = [CardLabel(world, object_id) for object_id in doubled]

            for object_id in doubled:
                card = world.object_manager.card_dict.get(object_id)
                if card is None or not card.IsOnField():
                    continue
                label = CardLabel(world, object_id)
                for route in LEAVE_PLAY_ROUTES:
                    with Silence():
                        try:
                            getattr(puzzle, route)(card.face)
                        except Exception as exc:
                            record["note"] += f" [{label} {route}: {type(exc).__name__}]"
                    if not card.IsOnField():
                        record["left_play"].append(f"{label} by {route}")
                        break
                else:
                    record["note"] += f" [{label}: stayed in play]"

            record["batches"] = watcher.batches
        record["digest"] = game.Digest()
        record["prompt_answers"] = game.policy.answered
    except Exception as exc:
        record["note"] = f"{type(exc).__name__}: {exc}"
    return record


def DriveOne(card_id: str, pick: int = 0) -> Dict[str, Any]:
    return DriveBoard(card_id.split("+"), pick=pick)


################################################################################
# Digest: does the order matter?

def DigestOne(card_id: str) -> Dict[str, Any]:
    """Drive the card out of play twice, picking a different candidate each time.

    Both runs travel the whole prompt -- `AskForcedOrder`, `ForcedOrderLabels`,
    `ChooseAbilities`, the device, the policy -- and differ only in the index the
    first player answered with. The comparison is `World.CalculateDigest()`,
    which is the project's oracle: every card, its zone, index, owner, host,
    face-up state and named state fields.

    A card whose two orders disagree is a finding, not a fix. It would mean the
    two cleanups are not interchangeable and the prompt is asking a real
    question.
    """
    first = DriveOne(card_id, pick=0)
    second = DriveOne(card_id, pick=1)
    picks_first = [batch.get("chosen", -1) for batch in first["batches"]]
    picks_second = [batch.get("chosen", -1) for batch in second["batches"]]
    return {
        "card_id": card_id,
        "name": first.get("name", ""),
        "prompts": first.get("prompt_answers", 0),
        "prompts_other_order": second.get("prompt_answers", 0),
        "picks_pick0": picks_first,
        "picks_pick1": picks_second,
        # Without this the identical digests prove nothing: two runs that made
        # the same choice are bound to agree.
        "orders_differed": picks_first != picks_second,
        "identical": bool(first["digest"]) and first["digest"] == second["digest"],
        # The digests themselves are a card dump per run; the report carries
        # their hashes and the verdict, which is what a reader can act on.
        "sha_pick0": Sha(first["digest"]),
        "sha_pick1": Sha(second["digest"]),
        "note": first["note"] or second["note"],
    }


def Sha(text: str) -> str:
    import hashlib
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


################################################################################
# Self-play

def SelfPlayOne(campaign: str, heroes: Sequence[str], seed: int, max_steps: int,
                policy: str, pick: int) -> Dict[str, Any]:
    """One headless game, with every multi-candidate forced batch written down.

    `probe_forced_selection` counts these; this one names them, because "the
    prompt fired" and "the prompt fired on two Temp0 cleanups of one card" are
    different claims and only the second is MARVEL-95.
    """
    from tools.determinism.headless import build_decide, run_headless

    watcher = Watcher()
    with watcher:
        # `build_decide` answers everything, including the ordering prompt. The
        # order the prompt is answered in is `pick` only where the harness can
        # steer it; see `PickPolicy` below.
        result = run_headless(campaign, list(heroes), seed, max_steps=max_steps,
                              decide=build_decide(policy, pick))
    return {
        "campaign": campaign, "heroes": list(heroes), "seed": seed,
        "steps": len(result.steps), "error": result.error,
        "run_digest": result.digest(),
        "batches": watcher.batches,
    }


################################################################################
# Shards
#
# The engine is a process-global singleton -- `Engine.game`, `CardsDB`, the
# config -- so parallelism here is subprocesses, never threads. The threads
# below only wait on them.

def RunShard(mode: str, argument: str) -> List[Dict[str, Any]]:
    Boot()
    if mode == "scan":
        return [ScanOne(card_id) for card_id in argument.split(",")]
    if mode == "drive":
        return [DriveOne(card_id) for card_id in argument.split(",")]
    if mode == "digest":
        return [DigestOne(card_id) for card_id in argument.split(",")]
    if mode == "selfplay":
        campaign, heroes, seed, max_steps, policy, pick = argument.split("|")
        return [SelfPlayOne(campaign, heroes.split(","), int(seed),
                            int(max_steps), policy, int(pick))]
    raise ValueError(f"unknown mode {mode!r}")


def Spawn(mode: str, argument: str) -> List[Dict[str, Any]]:
    from tools.determinism.pinned_env import build_env

    proc = subprocess.run(
        [sys.executable, "-m", "tools.determinism.probe_temp0_order",
         "--child", mode, argument],
        capture_output=True, text=True, env=build_env(), cwd=os.getcwd(),
        errors="replace",
    )
    for line in proc.stdout.splitlines():
        if line.startswith(MARKER):
            return json.loads(line[len(MARKER):])
    raise RuntimeError(
        f"no result from a {mode} shard\n"
        f"argument: {argument[:200]}\n"
        f"stderr tail: {proc.stderr[-1500:]}")


def Shard(items: Sequence[str], count: int) -> List[List[str]]:
    if count <= 1:
        return [list(items)]
    size = (len(items) + count - 1) // count
    return [list(items[i:i + size]) for i in range(0, len(items), size)]


def Parallel(mode: str, arguments: Sequence[str], workers: int) -> List[Dict[str, Any]]:
    if workers <= 1:
        return [record for argument in arguments
                for record in RunShard(mode, argument)]

    from concurrent.futures import ThreadPoolExecutor

    with ThreadPoolExecutor(max_workers=workers) as pool:
        results = list(pool.map(lambda argument: Spawn(mode, argument), arguments))
    return [record for chunk in results for record in chunk]


def OverCards(mode: str, card_ids: Sequence[str], workers: int) -> List[Dict[str, Any]]:
    chunks = Shard(card_ids, max(workers, 1))
    return Parallel(mode, [",".join(chunk) for chunk in chunks], workers)


################################################################################
# Report

def Describe1(record: Dict[str, Any]) -> str:
    return f"{record['card_id']:24s} {record.get('name', ''):34s}"


def RunScan(card_ids: Sequence[str], workers: int) -> List[Dict[str, Any]]:
    print(f"\nscan: {len(card_ids)} cards, each put into play alone; counting the "
          f"local Temp0/Temp0UI cleanups it leaves on every face in the world")
    return OverCards("scan", card_ids, workers)


def RunSelfPlay(workers: int, max_steps: int, policy: str, matrix: str,
                policy_seed: int = 0) -> List[Dict[str, Any]]:
    from tools.determinism.check_runs import MATRIX_SMOKE, MATRIX_WIDE

    cases = MATRIX_SMOKE if matrix == "smoke" else MATRIX_WIDE
    arguments = [
        f"{campaign}|{','.join(heroes)}|{seed}|{max_steps}|{policy}|{policy_seed}"
        for campaign, heroes, seed in cases]
    print(f"self-play: {len(arguments)} games, {matrix} matrix, policy {policy}"
          f" (policy seed {policy_seed})")
    return Parallel("selfplay", arguments, workers)


def ScanStage(card_ids: Sequence[str], workers: int,
              payload: Dict[str, Any]) -> Any:
    """The enumeration. Returns the single-card boards, the carriers, the appliers."""
    scan = RunScan(card_ids, workers)
    unplayable = [r for r in scan if not r["in_play"]]
    carriers = sorted((r for r in scan if r["carried"] >= 2),
                      key=lambda r: (-r["carried"], r["card_id"]))
    appliers = sorted((r for r in scan if r["contributed"] >= 1),
                      key=lambda r: r["card_id"])

    print(f"    {len(scan) - len(unplayable)} entered play, "
          f"{len(unplayable)} could not be put into play")
    print(f"    {len(carriers)} reach two cleanups on one face **alone**")
    for record in carriers:
        print(f"        {Describe1(record)} {record['carried']} on one face")
    print(f"    {len(appliers)} put a cleanup on some other face (appliers)")

    payload.update({
        "scanned": len(scan),
        "entered_play": len(scan) - len(unplayable),
        "carriers": carriers,
        "appliers": [{"card_id": r["card_id"], "name": r["name"],
                      "contributed": r["contributed"], "targets": r["targets"]}
                     for r in appliers],
        "unplayable": [{"card_id": r["card_id"], "name": r["name"],
                        "note": r["note"]} for r in unplayable],
    })
    return [r["card_id"] for r in carriers], carriers, appliers


def main(argv: "List[str]|None" = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cards", default="",
                        help="comma-separated card ids or `a+b` boards; "
                             "default is the whole pool")
    parser.add_argument("--workers", type=int, default=8,
                        help="subprocesses to shard across (1 runs in-process)")
    parser.add_argument("--json", dest="json_out", default="")
    parser.add_argument("--stage", default="all",
                        choices=("all", "selfplay", "scan"))
    parser.add_argument("--matrix", default="wide", choices=("smoke", "wide"))
    parser.add_argument("--policy", default="first",
                        choices=("decline", "first", "random"))
    parser.add_argument("--max-steps", type=int, default=400)
    parser.add_argument("--policy-seed", type=int, default=0)
    parser.add_argument("--max-pairs", type=int, default=0,
                        help="stop after N applier pairs (0 = every pair)")
    args = parser.parse_args(argv)

    payload: Dict[str, Any] = {}
    failed = False
    workers = max(args.workers, 1)

    # An explicit `--cards` list is a question about those boards. It skips the
    # self-play and scan stages, which are about the pool rather than a board,
    # and runs in this process so the engine log is visible for triage.
    named = [x.strip() for x in args.cards.split(",") if x.strip()]
    if named:
        workers = 1
    else:
        Boot()

    ############################################################################
    # Self-play: what a real game reaches, with no board built for it.
    if not named and args.stage in ("all", "selfplay"):
        games = RunSelfPlay(min(workers, 8), args.max_steps, args.policy,
                            args.matrix, args.policy_seed)
        batches = [batch for game in games for batch in game["batches"]]
        temp0 = [batch for batch in batches if AllTemp0(batch)]
        print(f"    {len(batches)} multi-candidate forced batches, "
              f"{len(temp0)} of them all-Temp0")
        for batch in batches:
            mark = "Temp0" if AllTemp0(batch) else "mixed"
            print(f"        [{mark}] {batch['priority']}  {batch['card_ids']}  "
                  f"{batch['types']}  {batch['labels']}  "
                  f"{sorted(set(batch['where']))}")
        payload["selfplay"] = games
        payload["selfplay_temp0_batches"] = temp0

    if args.stage == "selfplay":
        Emit(payload, args.json_out)
        return 0

    ############################################################################
    # Scan: who carries cleanups, and who puts them on other faces.
    if named:
        boards, carriers, appliers = named, [], []
    else:
        boards, carriers, appliers = ScanStage(GeneratableCardIds(), workers, payload)

    if args.stage == "scan":
        Emit(payload, args.json_out)
        return 0

    ############################################################################
    # Drive: single-card boards, then every pair of appliers.
    applier_ids = [r["card_id"] for r in appliers]
    pairs = [f"{a}+{b}" for index, a in enumerate(applier_ids)
             for b in applier_ids[index + 1:]]
    if args.max_pairs:
        pairs = pairs[:args.max_pairs]

    todo = boards + pairs
    print(f"\ndrive: {len(boards)} single-card board(s) and {len(pairs)} applier "
          f"pairs, each built and then taken apart")
    driven = OverCards("drive", todo, workers)
    prompted = [r for r in driven if any(AllTemp0(b) for b in r["batches"])]
    for record in prompted:
        print(f"    PROMPT {Describe1(record)} doubled={record['doubled']} "
              f"left={record['left_play']}")
    print(f"    {len(prompted)} of {len(driven)} boards reached an all-Temp0 "
          f"ordering prompt")
    payload["driven_count"] = len(driven)
    payload["prompted"] = prompted
    payload["doubled_but_silent"] = [
        {"card_id": r["card_id"], "doubled": r["doubled"], "note": r["note"],
         "left_play": r["left_play"]}
        for r in driven if r["doubled"] and r not in prompted]

    ############################################################################
    # Digest: does either order change the state?
    if prompted:
        ids = [r["card_id"] for r in prompted]
        print(f"\ndigest: both orders over {len(ids)} prompting boards")
        checked = OverCards("digest", ids, workers)
        payload["digest"] = checked
        vacuous = [r for r in checked if not r["orders_differed"]]
        for record in checked:
            verdict = "same" if record["identical"] else "DIFFERENT"
            control = "" if record["orders_differed"] else "  (both runs chose alike)"
            print(f"    {verdict:9s} {Describe1(record)} "
                  f"{record['prompts']}/{record['prompts_other_order']} prompt(s) "
                  f"{record['sha_pick0'][:12]} {record['sha_pick1'][:12]}{control}")
            if not record["identical"]:
                failed = True
        print(f"    {len(checked) - len(vacuous)} of {len(checked)} boards had the "
              f"two runs answer differently; {len(vacuous)} did not and prove "
              f"nothing on their own")
        payload["vacuous"] = [r["card_id"] for r in vacuous]
        if failed:
            print("\nSTOP: an order changed the state digest. The two abilities "
                  "are not interchangeable and the prompt is not spurious.")

    Emit(payload, args.json_out)
    return 1 if failed else 0


def Emit(payload: Dict[str, Any], path: str) -> None:
    if path:
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, indent=1, sort_keys=True)
        print(f"\nwrote {path}")


if __name__ == "__main__":
    if len(sys.argv) > 3 and sys.argv[1] == "--child":
        print(MARKER + json.dumps(RunShard(sys.argv[2], sys.argv[3])))
        raise SystemExit(0)
    raise SystemExit(main())
