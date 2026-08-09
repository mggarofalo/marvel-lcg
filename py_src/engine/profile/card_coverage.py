"""What a game actually exercised, as opposed to what it merely contained.

The replay corpus is only worth what it reaches. A corpus that never exercises
forty percent of the cards cannot validate forty percent of the port, and
without measurement that is discovered at the end. This is the measurement.

Three levels, because "the card was in the deck" and "the card did something"
are different claims and the second is the one that validates a port:

  present       the card existed in the game at all
  entered play  it reached an in-play zone
  resolved      one of its abilities triggered *and resolved*

And the level that actually matters for the port: which `AbilityFactory` method
fired. The factory methods are long-tailed -- a few dozen carry most of the
uses, the tail runs past three hundred -- and the tail is where port bugs will
hide, so the tail is what has to be measured.

Attribution works by wrapping the factory. `Instrument` replaces every
`staticmethod` on `AbilityFactory` with a wrapper that stamps the returned
`Ability` with the method's name. A factory that calls another factory
overwrites the inner stamp, so an ability ends up carrying the *outermost*
method -- the one the card script actually named, and therefore the same name
`tools/cards/scripts.py` reads off the script's syntax tree. That alignment is
what lets `coverage_report.py` subtract one from the other.

Card abilities are cached per card id for the life of the process
(`CardsDB.ability_cache`), so instrumentation has to be installed before the
first card is built. `BotRunner.Run` does that before its game loop.

Nothing here writes to game state, reads a clock, or touches the RNG, and every
collection is sorted on the way out. Coverage is an observation; a game played
with it on must be byte-identical to the same game played without it.

Not to be confused with `coverage.py` in this package, which counts executions
of card-script source locations, is disabled in a release build, and measures
something else.
"""

import functools

from core import *
from engine.log import Log

CATEGORY_NAME = "COVERAGE"

# Marks a wrapper this module installed, so instrumenting twice does not stack
# wrappers and so an already-wrapped factory is recognised across module reloads.
FACTORY_MARK = "__card_coverage_factory__"


class CardCoverage:

    # Recording is off unless something asks for it. The wrappers are installed
    # only when it is on, so an ordinary game pays nothing for this file.
    is_enable: bool = False

    # True between `BeginGame` and `EndGame`. Replay verification re-executes a
    # finished game through the same engine paths, so the window has to close
    # before verification runs or every game is counted twice.
    is_recording: bool = False

    is_instrumented: bool = False

    entered_play: Dict[str, int] = {}
    resolved: Dict[str, int] = {}
    factories: Dict[str, int] = {}
    triggers: Dict[str, int] = {}
    ability_types: Dict[str, int] = {}
    stages: Dict[str, int] = {}

    ################################################################################
    # Setup

    @staticmethod
    def Enable() -> int:
        """Turn recording on, install the factory wrappers, and say what it found.

        Returns how many factory methods are instrumented. The count is also
        reported here rather than left to the caller, because it is the only
        thing that separates "coverage is on" from "coverage is on and
        attributing nothing", and a signal every caller has to remember to check
        is one no caller checks.
        """
        CardCoverage.is_enable = True
        count = CardCoverage.Instrument()
        CardCoverage.ReportInstrumented(count)
        return count

    @staticmethod
    def ReportInstrumented(count: int) -> None:
        """Announce the wrapping, loudly when it wrapped nothing.

        Zero means every ability will carry an empty `factory` and the report
        will show no factory reached -- which is indistinguishable, in the
        artefact, from a corpus that genuinely exercised nothing. That has to
        arrive as an error during the run, not as a puzzling report afterwards.

        `Log.Info`, not `Log.Debug`: `build.py` hardcodes `Build.release = True`
        and `Log.Debug` is `PrintNull` under it, so a debug-level trace here
        would never print at all.
        """
        if count > 0:
            Log.Info(CATEGORY_NAME, f"Instrumented {count} AbilityFactory methods")
            return
        Log.Assert(CATEGORY_NAME,
            "Card coverage instrumented 0 AbilityFactory methods. Every ability "
            "will be unattributed and the report will show no trigger reached, "
            "which is not the same thing as a corpus that reached none.")

    @staticmethod
    def Disable() -> None:
        """Stop recording. The wrappers stay: they are inert, and removing them
        after abilities have been built and cached would leave the already
        stamped ones inconsistent with the rest."""
        CardCoverage.is_enable = False
        CardCoverage.is_recording = False

    @staticmethod
    def Instrument() -> int:
        """Wrap `AbilityFactory` so every ability records what built it."""
        from game.ability.ability import Ability
        from game.ability.factory.ability_factory import AbilityFactory

        count = CardCoverage.InstrumentClass(AbilityFactory, Ability)
        CardCoverage.is_instrumented = True
        return count

    @staticmethod
    def InstrumentClass(owner: type, ability_type: type) -> int:
        """Wrap every public static method on `owner`. Returns how many there are.

        Idempotent: a wrapper carries `FACTORY_MARK`, and a marked method is
        counted and left alone rather than wrapped a second time.

        Split out from `Instrument` so the wrapping rules can be tested against
        a small stand-in class -- the real `AbilityFactory` is three hundred odd
        methods composed from forty mixins, and a test that boots it fails on
        the composition rather than on the rule being tested.
        """
        count = 0
        for name in dir(owner):
            if name.startswith("_"):
                continue
            raw = CardCoverage.FindStaticMethod(owner, name)
            if raw is None:
                continue
            count += 1
            function = raw.__func__
            if getattr(function, FACTORY_MARK, None) is not None:
                continue
            wrapper = CardCoverage.WrapFactory(ability_type, function, name)
            setattr(owner, name, staticmethod(wrapper))
        return count

    @staticmethod
    def FindStaticMethod(owner: type, name: str) -> 'staticmethod[Any, Any]|None':
        """The `staticmethod` object for `name`, wherever in the MRO it lives.

        `AbilityFactory` is a composition of forty-odd mixins, so almost nothing
        is in its own `__dict__`. Reading the attribute off the class instead
        would hand back a plain function with no way to tell a static method
        from anything else on the class.
        """
        for base in owner.__mro__:
            found = base.__dict__.get(name)
            if found is not None:
                return found if isinstance(found, staticmethod) else None
        return None

    @staticmethod
    def WrapFactory(ability_type: type, function: Callable[..., Any], factory_name: str) -> Callable[..., Any]:
        @functools.wraps(function)
        def wrapper(*args: Any, **kwargs: Any) -> Any:
            result = function(*args, **kwargs)
            CardCoverage.Stamp(ability_type, result, factory_name)
            return result

        setattr(wrapper, FACTORY_MARK, factory_name)
        return wrapper

    @staticmethod
    def Stamp(ability_type: type, result: Any, factory_name: str) -> None:
        """Attribute whatever came back to the factory that returned it.

        Most factories return one ability; a few return a list of them. Anything
        else -- a selector, a bool, None -- passes through untouched.
        """
        if isinstance(result, ability_type):
            result.factory = factory_name
        elif isinstance(result, (list, tuple)):
            for item in result:
                if isinstance(item, ability_type):
                    item.factory = factory_name

    ################################################################################
    # Recording

    @staticmethod
    def BeginGame() -> None:
        if not CardCoverage.is_enable:
            return
        CardCoverage.entered_play = {}
        CardCoverage.resolved = {}
        CardCoverage.factories = {}
        CardCoverage.triggers = {}
        CardCoverage.ability_types = {}
        CardCoverage.stages = {}
        CardCoverage.is_recording = True

    @staticmethod
    def RecordCardEnteredPlay(face: Any) -> None:
        """Called once a card has actually reached an in-play zone.

        The caller checks that, not this: a card whose enter-play was replaced
        or countered never arrives, and counting the attempt would report
        coverage the game did not have.
        """
        if not CardCoverage.is_recording:
            return
        paper = getattr(face, "paper", None)
        if paper is None:
            return
        card_id = paper.card_id
        CardCoverage.entered_play[card_id] = CardCoverage.entered_play.get(card_id, 0) + 1

        # Villains and main schemes carry a stage; everything else reports 0.
        # Which stages a corpus reaches is its own coverage question -- a corpus
        # that never gets past stage I never tests stage II's abilities.
        stage = getattr(face, "printed_stage", 0)
        if stage:
            CardCoverage.stages[card_id] = int(stage)

    @staticmethod
    def RecordAbilityResolved(ability: Any) -> None:
        """Called after an ability's operation has run to completion.

        This is the load-bearing measurement. Registering an ability, offering
        it, or paying for it are all cheap to reach and prove nothing about the
        operation a port has to reproduce.
        """
        if not CardCoverage.is_recording:
            return

        factory = getattr(ability, "factory", "")
        if factory:
            CardCoverage.factories[factory] = CardCoverage.factories.get(factory, 0) + 1

        # Rule and statistics abilities are built by `Ability(...)` directly and
        # belong to no card, so `paper` is None. They are still worth counting
        # under their trigger -- they are engine behaviour a port has to match.
        paper = getattr(ability, "paper", None)
        if paper is not None:
            card_id = paper.card_id
            CardCoverage.resolved[card_id] = CardCoverage.resolved.get(card_id, 0) + 1

        trigger = CardCoverage.TriggerName(getattr(ability, "when", None))
        if trigger:
            CardCoverage.triggers[trigger] = CardCoverage.triggers.get(trigger, 0) + 1

        ability_type = CardCoverage.AbilityTypeName(ability)
        if ability_type:
            CardCoverage.ability_types[ability_type] = CardCoverage.ability_types.get(ability_type, 0) + 1

    @staticmethod
    def TriggerName(when: Any) -> str:
        """The message class an ability triggers on, as a stable short name.

        `Ability.when` is usually one message class but can be a union of them
        ("interrupt on either of these"), which has no `__name__`. A union is
        named by its members, sorted, so the same union always spells the same.
        """
        if when is None:
            return ""
        name = getattr(when, "__name__", None)
        if isinstance(name, str):
            return name
        members = get_args(when)
        if members:
            return "|".join(sorted(getattr(m, "__name__", str(m)) for m in members))
        return str(when)

    @staticmethod
    def AbilityTypeName(ability: Any) -> str:
        """`AbilityType.name`, not its value: the identifier is stable, the
        value is display text ("Forced Response") that a rewording would move."""
        ability_type = getattr(ability, "type", None)
        if ability_type is None:
            return ""
        return str(getattr(ability_type, "name", ability_type))

    ################################################################################
    # The per-game record

    @staticmethod
    def EndGame(world: Any, *, seed: int, scenario: str, heroes: Sequence[str],
                outcome: str) -> Dict[str, Any]:
        """Close the recording window and return what this game exercised.

        `scenario` and `heroes` come from the caller rather than from the scene
        because the caller knows the keys the run was *launched* with -- what
        you would pass back to `-bot_scenario` to reach this coverage again. The
        scene stores display names.
        """
        record = CardCoverage.BuildRecord(
            world, seed=seed, scenario=scenario, heroes=heroes, outcome=outcome)
        CardCoverage.is_recording = False
        return record

    @staticmethod
    def BuildRecord(world: Any, *, seed: int, scenario: str, heroes: Sequence[str],
                    outcome: str) -> Dict[str, Any]:
        scene = getattr(world, "scene", None)
        campaign = getattr(scene, "campaign", None)
        rule = getattr(world, "rule", None)

        return {
            "seed": seed,
            "scenario": scenario,
            "heroes": list(heroes),
            # Seat order, not `const_players`. The latter is `world.players`,
            # which is rotated as the first player passes and *shrinks* when a
            # hero is eliminated -- so a two-handed game that ended with one
            # hero dead reported itself as a solo game, and the corpus looked
            # like it had covered a player count it never played.
            "player_count": len(getattr(world, "const_seat_order_players", []) or []),
            "expert": bool(getattr(campaign, "expert", False)),
            "challenges": sorted(getattr(campaign, "challenges", []) or []),
            "rules": sorted(getattr(scene, "rules", []) or []),
            "modes": {
                "heroic": int(getattr(rule, "mode_heroic", 0) or 0),
                "skirmish": bool(getattr(rule, "mode_skirmish", False)),
                "campaign": bool(getattr(rule, "mode_campaign", False)),
            },
            "outcome": outcome,
            "cards": {
                "present": CardCoverage.PresentCardIds(world),
                "entered_play": CardCoverage.SortedCounts(CardCoverage.entered_play),
                "resolved": CardCoverage.SortedCounts(CardCoverage.resolved),
            },
            "stages": CardCoverage.SortedCounts(CardCoverage.stages),
            "factories": CardCoverage.SortedCounts(CardCoverage.factories),
            "triggers": CardCoverage.SortedCounts(CardCoverage.triggers),
            "ability_types": CardCoverage.SortedCounts(CardCoverage.ability_types),
        }

    @staticmethod
    def PresentCardIds(world: Any) -> List[str]:
        """Every card id this game contained, in any zone, on either face.

        Read at the end from the object manager rather than accumulated as cards
        are created, so a card generated mid-game by an ability is counted too.
        Both printed faces count: `01001a` and `01001b` are separate entries in
        the dataset and can have separate scripts.
        """
        card_dict = getattr(getattr(world, "object_manager", None), "card_dict", None)
        if not card_dict:
            return []
        card_ids: Set[str] = set()
        for object_id in sorted(card_dict):
            for face in card_dict[object_id].printed_faces:
                card_ids.add(face.paper.card_id)
        return sorted(card_ids)

    @staticmethod
    def SortedCounts(counts: Dict[str, int]) -> Dict[str, int]:
        return {key: counts[key] for key in sorted(counts)}
