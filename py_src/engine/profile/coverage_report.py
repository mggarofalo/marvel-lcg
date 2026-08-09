"""Turn raw coverage observations into the report a corpus run is judged by.

`card_coverage.py` watches one game and writes down what it saw. This file
answers the question that matters: **what did the corpus fail to reach**. That
needs a universe to subtract from, and the universe is `datasets/cards/cards.json`
-- the card dataset built by `tools/cards/extract.py`, which records for every
card the `AbilityFactory` methods its script registers.

Two ranked lists come out, and they are the direct input to coverage-directed
generation:

  never-fired factories   ordered by how many card scripts register them. A
                          trigger eighty cards depend on is worth reaching
                          before one a single card uses.
  never-exercised cards   ordered by how many never-fired factories that card
                          would newly exercise -- a greedy set-cover score. Play
                          the card at the top and the most triggers light up.

The runtime and static namespaces line up exactly: `CardCoverage.Instrument`
stamps an ability with the name of the outermost `AbilityFactory` method that
built it, and `tools/cards/scripts.py` reads the same name off the card script's
syntax tree. `unit_test/test_card_coverage_play.py` asserts that nothing fires
at runtime which the dataset has never heard of, so a renamed factory method
fails a test instead of quietly shrinking the denominator.

Deliberately pure stdlib, like `game/world/digest.py`: it is imported by the bot
runner mid-corpus-run and by a command line tool that has not booted an engine,
and neither should have to care which.
"""

from __future__ import annotations

import json
import os
from dataclasses import dataclass, field
from typing import Any, Dict, Iterable, List, Sequence

COVERAGE_VERSION = 1

# Where `tools.cards.extract` writes the dataset, relative to `py_src/`.
DEFAULT_DATASET = os.path.join("..", "datasets", "cards", "cards.json")


################################################################################
# The universe


@dataclass
class Universe:
    """Every card the engine has a script for, and what that script registers.

    A card with no script has no ability to exercise, so counting it as
    unreached would put a permanent floor under the miss rate and make the
    number useless. `with_script` in the dataset's own counts is the same
    population.
    """

    source: str = ""
    dataset_version: int = 0
    # card_id -> the AbilityFactory methods its script names.
    card_factories: Dict[str, List[str]] = field(default_factory=dict)
    # AbilityFactory method -> how many card scripts name it. The ranking weight.
    factory_cards: Dict[str, int] = field(default_factory=dict)
    names: Dict[str, str] = field(default_factory=dict)
    packs: Dict[str, str] = field(default_factory=dict)

    @property
    def cards(self) -> List[str]:
        return sorted(self.card_factories)

    @property
    def factories(self) -> List[str]:
        return sorted(self.factory_cards)


class DatasetMissing(Exception):
    """`datasets/cards/cards.json` is absent or not the shape this expects."""


def LoadUniverse(path: str = DEFAULT_DATASET) -> Universe:
    """Read the card dataset. Raises `DatasetMissing` rather than guessing."""
    if not os.path.exists(path):
        raise DatasetMissing(
            f"{path} is missing. It is written by `python -m tools.cards.extract`; "
            "without it a coverage report can count what was reached but not what "
            "was missed."
        )
    try:
        with open(path, encoding="utf-8") as handle:
            document = json.load(handle)
    except (OSError, ValueError) as exc:
        raise DatasetMissing(f"{path} is unreadable: {exc}") from exc

    cards = document.get("cards")
    if not isinstance(cards, list):
        raise DatasetMissing(f"{path} has no 'cards' array")

    universe = Universe(
        source=path.replace(os.sep, "/"),
        dataset_version=int(document.get("dataset_version") or 0),
    )
    for card in cards:
        if not isinstance(card, dict):
            continue
        script = (card.get("engine") or {}).get("script")
        if not script:
            continue
        card_id = card.get("card_id")
        if not card_id:
            continue
        factories = sorted(script.get("ability_factories") or [])
        universe.card_factories[card_id] = factories
        universe.names[card_id] = card.get("name") or ""
        universe.packs[card_id] = card.get("pack") or ""
        for factory in factories:
            universe.factory_cards[factory] = universe.factory_cards.get(factory, 0) + 1
    return universe


################################################################################
# Aggregation


def _Bump(into: Dict[str, int], key: str, value: int = 1) -> None:
    into[key] = into.get(key, 0) + value


def _MergeCounts(into: Dict[str, int], counts: Any) -> None:
    if isinstance(counts, dict):
        for key, value in counts.items():
            _Bump(into, str(key), int(value))


def _Sorted(counts: Dict[str, int]) -> Dict[str, int]:
    """Key order is part of the artefact -- a diff between two runs should show
    what changed, not that a dict was built in a different order."""
    return {key: counts[key] for key in sorted(counts)}


@dataclass
class Totals:
    present: Dict[str, int] = field(default_factory=dict)
    entered_play: Dict[str, int] = field(default_factory=dict)
    resolved: Dict[str, int] = field(default_factory=dict)
    factories: Dict[str, int] = field(default_factory=dict)
    triggers: Dict[str, int] = field(default_factory=dict)
    ability_types: Dict[str, int] = field(default_factory=dict)


def Accumulate(games: Sequence[Dict[str, Any]]) -> Totals:
    """Roll per-game records up. `present` counts games, not copies: a card in a
    forty-card deck is present once per game however many times it was shuffled."""
    totals = Totals()
    for game in games:
        cards = game.get("cards") or {}
        for card_id in cards.get("present") or []:
            _Bump(totals.present, str(card_id))
        _MergeCounts(totals.entered_play, cards.get("entered_play"))
        _MergeCounts(totals.resolved, cards.get("resolved"))
        _MergeCounts(totals.factories, game.get("factories"))
        _MergeCounts(totals.triggers, game.get("triggers"))
        _MergeCounts(totals.ability_types, game.get("ability_types"))
    return totals


def Reached(games: Sequence[Dict[str, Any]]) -> Dict[str, Any]:
    """What the corpus covered of the things a game is configured *by*.

    Card coverage says nothing about whether the corpus ever played four-handed
    or ever saw an expert deck, and a port that only ever ran one-handed
    standard is not validated for the rest.
    """
    scenarios: Dict[str, int] = {}
    heroes: Dict[str, int] = {}
    player_counts: Dict[str, int] = {}
    heroic: Dict[str, int] = {}
    challenges: Dict[str, int] = {}
    stages: Dict[str, Dict[str, int]] = {}
    expert = 0
    skirmish = 0
    campaign = 0

    for game in games:
        _Bump(scenarios, str(game.get("scenario") or ""))
        for hero in game.get("heroes") or []:
            _Bump(heroes, str(hero))
        _Bump(player_counts, str(game.get("player_count") or 0))
        for challenge in game.get("challenges") or []:
            _Bump(challenges, str(challenge))
        modes = game.get("modes") or {}
        _Bump(heroic, str(modes.get("heroic") or 0))
        if game.get("expert"):
            expert += 1
        if modes.get("skirmish"):
            skirmish += 1
        if modes.get("campaign"):
            campaign += 1
        for card_id, stage in (game.get("stages") or {}).items():
            entry = stages.setdefault(str(card_id), {"stage": int(stage), "games": 0})
            entry["games"] += 1

    return {
        "scenarios": _Sorted(scenarios),
        "heroes": _Sorted(heroes),
        "player_counts": _Sorted(player_counts),
        "challenges": _Sorted(challenges),
        "difficulty": {
            "expert": expert,
            "standard": len(games) - expert,
            "heroic": _Sorted(heroic),
            "skirmish": skirmish,
            "campaign": campaign,
        },
        "stages": {key: stages[key] for key in sorted(stages)},
    }


################################################################################
# The ranked lists


def NeverFiredFactories(universe: Universe, fired: Iterable[str]) -> List[Dict[str, Any]]:
    """Registered by some card script, never reached by the corpus.

    Ranked by how many scripts depend on the factory, so the top of the list is
    the trigger whose absence leaves the most cards unvalidated.
    """
    seen = set(fired)
    missing = [name for name in universe.factories if name not in seen]
    missing.sort(key=lambda name: (-universe.factory_cards[name], name))
    return [{"factory": name, "cards": universe.factory_cards[name]} for name in missing]


def NeverExercisedCards(universe: Universe, resolved: Iterable[str],
                        never_fired: Iterable[str]) -> List[Dict[str, Any]]:
    """Cards no ability of which ever resolved.

    "Present in a deck" is not exercised -- that is the whole point of the
    metric, so the input here is `resolved`, not `present`.

    The score is how many never-fired factories the card would newly light up.
    It is a greedy set-cover weight rather than a proof of optimality: reaching
    the top card does not guarantee the next one is still second. Recompute
    after each generation round.
    """
    exercised = set(resolved)
    unfired = set(never_fired)

    ranked: List[Dict[str, Any]] = []
    for card_id in universe.cards:
        if card_id in exercised:
            continue
        missing = sorted(set(universe.card_factories[card_id]) & unfired)
        ranked.append({
            "card_id": card_id,
            "name": universe.names.get(card_id, ""),
            "pack": universe.packs.get(card_id, ""),
            "score": len(missing),
            "unfired": missing,
        })
    ranked.sort(key=lambda entry: (-entry["score"], entry["card_id"]))
    return ranked


################################################################################
# The document


def Build(games: Sequence[Dict[str, Any]],
          *,
          generator: str,
          engine_version: str,
          universe: Universe | None,
          universe_error: str = "") -> Dict[str, Any]:
    """The whole report. `universe=None` still produces one.

    A missing dataset costs the two ranked lists, not the run: the observations
    are the expensive half and they are still worth writing down. The document
    says so out loud rather than emitting empty lists, which would read as
    "nothing was missed".
    """
    totals = Accumulate(games)

    document: Dict[str, Any] = {
        "coverage_version": COVERAGE_VERSION,
        "generator": generator,
        "engine_version": engine_version,
        "games": list(games),
        "totals": {
            "games": len(games),
            "cards": {
                "present": len(totals.present),
                "entered_play": len(totals.entered_play),
                "resolved": len(totals.resolved),
            },
            "factories": {"fired": len(totals.factories)},
        },
        "counts": {
            "cards_present": _Sorted(totals.present),
            "cards_entered_play": _Sorted(totals.entered_play),
            "cards_resolved": _Sorted(totals.resolved),
            "factories": _Sorted(totals.factories),
            "triggers": _Sorted(totals.triggers),
            "ability_types": _Sorted(totals.ability_types),
        },
        "reached": Reached(games),
    }

    if universe is None:
        document["universe"] = {
            "available": False,
            "reason": universe_error or "no card dataset was loaded",
        }
        return document

    never_fired = NeverFiredFactories(universe, totals.factories)
    never_fired_names = [entry["factory"] for entry in never_fired]

    document["universe"] = {
        "available": True,
        "source": universe.source,
        "dataset_version": universe.dataset_version,
        "cards": len(universe.card_factories),
        "factories": len(universe.factory_cards),
    }
    document["totals"]["cards"]["universe"] = len(universe.card_factories)
    # Not every card id a game resolves is in the universe. A vanilla minion, a
    # plain resource and a main scheme with only printed stats have no script --
    # their behaviour is the engine's, not a card's -- and the engine's two
    # `rule_*` pseudo-cards are not cards at all. Counting them in the numerator
    # over a script-only denominator would overstate the rate, so the ratio uses
    # the intersection and the raw observation is kept beside it.
    document["totals"]["cards"]["resolved_in_universe"] = len(
        set(totals.resolved) & set(universe.card_factories))
    document["totals"]["factories"]["universe"] = len(universe.factory_cards)
    document["never_fired_factories"] = never_fired
    document["never_exercised_cards"] = NeverExercisedCards(
        universe, totals.resolved, never_fired_names)
    return document


def Summarize(document: Dict[str, Any]) -> str:
    """One paragraph a human reads before deciding whether to keep generating."""
    totals = document.get("totals") or {}
    cards = totals.get("cards") or {}
    factories = totals.get("factories") or {}

    def ratio(part: Any, whole: Any) -> str:
        if not isinstance(whole, int) or whole <= 0:
            return str(part)
        return f"{part}/{whole} ({part / whole * 100:.1f}%)"

    # `resolved_in_universe` is absent when no dataset was loaded, in which case
    # there is no denominator either and the raw observation is all there is.
    resolved = cards.get("resolved_in_universe", cards.get("resolved", 0))

    lines = [
        f"games:      {totals.get('games', 0)}",
        f"present:    {cards.get('present', 0)} card ids",
        f"in play:    {cards.get('entered_play', 0)} card ids",
        f"resolved:   {ratio(resolved, cards.get('universe'))} card ids",
        f"factories:  {ratio(factories.get('fired', 0), factories.get('universe'))}",
    ]

    top_factories = (document.get("never_fired_factories") or [])[:5]
    if top_factories:
        lines.append("worst unreached triggers:")
        for entry in top_factories:
            lines.append(f"  {entry['factory']} ({entry['cards']} cards)")
    return "\n".join(lines)


def GamesOf(documents: Iterable[Dict[str, Any]]) -> List[Dict[str, Any]]:
    """Every per-game record across several run artefacts.

    Merging is a plain concatenation because a game record is self-describing --
    seed, scenario, heroes and outcome are all on it. Two runs of the same seed
    do genuinely appear twice; that is a fact about the corpus, not a bug in the
    merge, and it shows up as a doubled count rather than as hidden coverage.
    """
    games: List[Dict[str, Any]] = []
    for document in documents:
        found = document.get("games")
        if isinstance(found, list):
            games.extend(found)
    return games
