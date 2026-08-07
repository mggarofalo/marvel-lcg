"""Authoring scenarios in Gherkin, compiled to `SpecCase`.

Scenarios are written as `.feature` files because the trusted suite has to
outlive the Python engine. Reqnroll (the maintained SpecFlow successor) binds
step text to C# methods with `[Given(@"...")]`, so the same file that validates
against the Python engine today is the file the C# engine is held to later.
Nothing here depends on Reqnroll; what it depends on is that the *step text* is
a stable, closed vocabulary rather than free prose.

That vocabulary is `STEP_TABLE` below. A step that matches nothing is a parse
error naming the file and line -- a scenario never half-runs, and a typo never
becomes a silently skipped assertion.

Supported: `Feature`, `Background`, `Scenario`, `Scenario Outline` with
`Examples`, `Given`/`When`/`Then`/`And`/`But`, `@tags`, and `#` comments. Doc
strings and data tables are not supported; a step that needs a list takes a
comma-separated one.

    Feature: Spider-Man basics

      Background:
        Given the scenario "rhino"
        And the hero "spider_man"

      Scenario: A basic attack deals the hero's ATK
        Given "01001a" is in hero form
        When the player attacks "Rhino in VillainArea"
        Then "Rhino in VillainArea" has 12 health
"""

from __future__ import annotations

import os
import re
from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Optional, Pattern, Sequence, Tuple

from tools.spec.case import (
    GivenStep, SourceDigest, SpecCase, SpecCaseError, ThenStep, WhenStep)
from tools.spec.resolve import SplitRefs


class GherkinError(SpecCaseError):
    """A `.feature` file that cannot be compiled, with the line that broke it."""


################################################################################
# Step vocabulary
#
# Each entry is (pattern, builder). A builder returns one of:
#   ("setting", key, value)   scenario-level configuration
#   ("given", GivenStep)
#   ("when", WhenStep)
#   ("then", ThenStep)
#
# `Q` is a quoted argument; quotes are required so a card called
# "Hard to Keep Down" is unambiguous.

Q = r'"([^"]*)"'
N = r'(-?\d+)'
LIST = r'(.+)'


def Rx(pattern: str) -> Pattern[str]:
    return re.compile(r"^" + pattern + r"$", re.IGNORECASE)


def BoolOf(text: str) -> bool:
    return text.strip().lower() not in ("no", "not", "false", "0")


Table = List[Tuple[Pattern[str], Callable[..., Any]]]

# One table per clause. They are kept apart because the same sentence means
# different things in different clauses: `"01097b" has 3 threat` sets up the
# board under Given and asserts it under Then. Gherkin's own keywords are the
# only thing that can tell them apart, so the parser uses them.

GIVEN_TABLE: Table = [

    # -- scenario configuration -------------------------------------------
    (Rx(r'the scenario ' + Q),
     lambda name: ("setting", "scenario", name)),
    (Rx(r'the hero ' + Q),
     lambda name: ("setting", "hero", name)),
    (Rx(r'the heroes ' + LIST),
     lambda names: ("setting", "heroes", SplitRefs(names))),
    (Rx(r'the seed is ' + N),
     lambda value: ("setting", "seed", int(value))),
    (Rx(r'the difficulty is expert'),
     lambda: ("setting", "expert", True)),

    # -- Given: zone fills -------------------------------------------------
    (Rx(r'the hand contains ' + LIST),
     lambda cards: ("given", GivenStep("hand", SplitRefs(cards)))),
    (Rx(r'the player deck contains ' + LIST),
     lambda cards: ("given", GivenStep("player_deck", SplitRefs(cards)))),
    (Rx(r'the player discard pile contains ' + LIST),
     lambda cards: ("given", GivenStep("player_discard", SplitRefs(cards)))),
    (Rx(r'the encounter deck contains ' + LIST),
     lambda cards: ("given", GivenStep("encounter_deck", SplitRefs(cards)))),
    (Rx(r'the encounter discard pile contains ' + LIST),
     lambda cards: ("given", GivenStep("encounter_discard", SplitRefs(cards)))),
    (Rx(r'the set aside deck contains ' + LIST),
     lambda cards: ("given", GivenStep("player_set_aside", SplitRefs(cards)))),

    # -- Given: magnitudes -------------------------------------------------
    (Rx(Q + r' has ' + N + r' damage'),
     lambda card, value: ("given", GivenStep("damage", (card,), value=int(value)))),
    (Rx(Q + r' is healed (?:for |by )?' + N),
     lambda card, value: ("given", GivenStep("heal", (card,), value=int(value)))),
    (Rx(Q + r' has ' + N + r' threat'),
     lambda card, value: ("given", GivenStep("threat", (card,), value=int(value)))),
    (Rx(Q + r' has ' + N + r' ' + Q + r' counters?'),
     lambda card, value, name: ("given", GivenStep(
         "counters", (card,), value=int(value), name=name))),
    (Rx(Q + r' has ' + N + r' ' + Q + r' tokens?'),
     lambda card, value, name: ("given", GivenStep(
         "tokens", (card,), value=int(value), name=name))),

    # -- Given: states -----------------------------------------------------
    (Rx(Q + r' is stunned'),
     lambda card: ("given", GivenStep("stunned", (card,)))),
    (Rx(Q + r' is confused'),
     lambda card: ("given", GivenStep("confused", (card,)))),
    (Rx(Q + r' is tough'),
     lambda card: ("given", GivenStep("tough", (card,)))),
    (Rx(Q + r' is exhausted'),
     lambda card: ("given", GivenStep("exhausted", (card,)))),
    (Rx(Q + r' is ready'),
     lambda card: ("given", GivenStep("ready", (card,)))),
    (Rx(Q + r' is discarded'),
     lambda card: ("given", GivenStep("discarded", (card,)))),
    (Rx(Q + r' is in play'),
     lambda card: ("given", GivenStep("in_play", (card,)))),
    (Rx(Q + r' is revealed'),
     lambda card: ("given", GivenStep("revealed", (card,)))),
    (Rx(Q + r' is in hero form'),
     lambda card: ("given", GivenStep("hero_form", (card,)))),
    (Rx(Q + r' is in alter-ego form'),
     lambda card: ("given", GivenStep("alter_ego_form", (card,)))),
    (Rx(r'the player draws ' + N + r' cards?'),
     lambda value: ("given", GivenStep("draw", value=int(value)))),
]

WHEN_TABLE: Table = [
    (Rx(r'the player attacks ' + Q),
     lambda target: ("when", WhenStep(option="Attack", targets=(target,)))),
    (Rx(r'the player thwarts ' + Q),
     lambda target: ("when", WhenStep(option="Thwart", targets=(target,)))),
    (Rx(r'the player defends against ' + Q),
     lambda target: ("when", WhenStep(option="Defense", targets=(target,)))),
    (Rx(r'the player changes form'),
     lambda: ("when", WhenStep(option="Change_Form"))),
    (Rx(r'the player plays ' + Q + r' targeting ' + Q),
     lambda card, target: ("when", WhenStep(
         option="Play", card=card, targets=(target,)))),
    (Rx(r'the player plays ' + Q),
     lambda card: ("when", WhenStep(option="Play", card=card))),
    (Rx(r'the player chooses ' + Q + r' on ' + Q + r' targeting ' + LIST),
     lambda option, card, targets: ("when", WhenStep(
         option=option, card=card, targets=SplitRefs(targets)))),
    (Rx(r'the player chooses ' + Q + r' targeting ' + LIST),
     lambda option, targets: ("when", WhenStep(
         option=option, targets=SplitRefs(targets)))),
    (Rx(r'the player chooses ' + Q + r' on ' + Q),
     lambda option, card: ("when", WhenStep(option=option, card=card))),
    (Rx(r'the player chooses ' + Q),
     lambda option: ("when", WhenStep(option=option))),
    (Rx(r'the player passes'),
     lambda: ("when", WhenStep(pass_priority=True))),
]

THEN_TABLE: Table = [
    (Rx(Q + r' has ' + N + r' health'),
     lambda card, value: ("then", ThenStep(card, "health", int(value)))),
    (Rx(Q + r' has ' + N + r' damage'),
     lambda card, value: ("then", ThenStep(card, "damage", int(value)))),
    (Rx(Q + r' has ' + N + r' threat'),
     lambda card, value: ("then", ThenStep(card, "threat", int(value)))),
    (Rx(Q + r' has ' + N + r' ' + Q + r' counters?'),
     lambda card, value, name: ("then", ThenStep(card, f"counter:{name}", int(value)))),
    (Rx(Q + r' has ' + N + r' ' + Q + r' tokens?'),
     lambda card, value, name: ("then", ThenStep(card, f"token:{name}", int(value)))),
    (Rx(Q + r' is in the ' + Q),
     lambda card, zone: ("then", ThenStep(card, "zone", zone))),
    (Rx(Q + r' is (not )?in play'),
     lambda card, negated: ("then", ThenStep(card, "in_play", not negated))),
    (Rx(Q + r' is (not )?exhausted'),
     lambda card, negated: ("then", ThenStep(card, "exhausted", not negated))),
    (Rx(Q + r' is (not )?ready'),
     lambda card, negated: ("then", ThenStep(card, "ready", not negated))),
    (Rx(Q + r' is (not )?stunned'),
     lambda card, negated: ("then", ThenStep(card, "stunned", not negated))),
    (Rx(Q + r' is (not )?confused'),
     lambda card, negated: ("then", ThenStep(card, "confused", not negated))),
    (Rx(Q + r' is (not )?tough'),
     lambda card, negated: ("then", ThenStep(card, "tough", not negated))),
    (Rx(Q + r' has ' + N + r' ' + Q),
     lambda card, value, prop: ("then", ThenStep(card, prop, int(value)))),
    (Rx(r'the player has ' + N + r' cards? in hand'),
     lambda value: ("then", ThenStep("player", "hand_size", int(value)))),
    (Rx(r'player ' + N + r' has ' + N + r' cards? in hand'),
     lambda who, value: ("then", ThenStep(f"player {who}", "hand_size", int(value)))),
    (Rx(r'the player has ' + N + r' cards? in the deck'),
     lambda value: ("then", ThenStep("player", "deck_size", int(value)))),
    (Rx(r'the player has ' + N + r' cards? in the discard pile'),
     lambda value: ("then", ThenStep("player", "discard_size", int(value)))),
    (Rx(r'the player is (not )?eliminated'),
     lambda negated: ("then", ThenStep("player", "eliminated", not negated))),
    (Rx(r'the game is (not )?over'),
     lambda negated: ("then", ThenStep("game", "game_over", not negated))),
    (Rx(r'the players (?:have )?(lost|won)'),
     lambda outcome: ("then", ThenStep("game", "players_won", outcome.lower() == "won"))),
    (Rx(r'it is round ' + N),
     lambda value: ("then", ThenStep("game", "round", int(value)))),
]

TABLES: Dict[str, Table] = {
    "given": GIVEN_TABLE,
    "when": WHEN_TABLE,
    "then": THEN_TABLE,
}


def CompileStep(keyword: str, text: str) -> Any:
    for pattern, build in TABLES[keyword]:
        match = pattern.match(text.strip())
        if match:
            return build(*match.groups())
    return None


def KnownSteps(keyword: str) -> List[str]:
    """The patterns a clause accepts, for an error message."""
    return [pattern.pattern.strip("^$") for pattern, _ in TABLES[keyword]]


################################################################################
# Parser

KEYWORD = re.compile(
    r"^(Feature|Background|Scenario Outline|Scenario Template|Scenario|Examples|Scenarios)\s*:\s*(.*)$",
    re.IGNORECASE)
STEP = re.compile(r"^(Given|When|Then|And|But|\*)\s+(.*)$", re.IGNORECASE)
PLACEHOLDER = re.compile(r"<([^<>]+)>")


@dataclass
class Draft:
    """A scenario being assembled: its tags and its steps, still as text.

    Steps stay as written until the whole scenario has been read, because a
    Scenario Outline's steps only become meaningful once an Examples row has
    been substituted into them. Keeping one representation keeps step order
    exactly as authored.
    """

    name: str = ""
    line: int = 0
    tags: List[str] = field(default_factory=list)
    steps: List[Tuple[str, str, int]] = field(default_factory=list)
    """(clause, text, line number), clause being given / when / then."""

    def Copy(self) -> "Draft":
        return Draft(name=self.name, line=self.line,
                     tags=list(self.tags), steps=list(self.steps))


def ParseFeature(text: str, *, path: str = "") -> List[SpecCase]:
    """Compile a `.feature` file into cases.

    Raises `GherkinError`, naming the line, for any step the vocabulary does
    not cover. A scenario compiles completely or not at all.
    """
    where = path or "<feature>"
    digest = SourceDigest(text)

    feature_name = ""
    background = Draft()
    current: Optional[Draft] = None
    outline: Optional[Draft] = None
    pending_tags: List[str] = []
    examples_header: List[str] = []
    in_background = False
    in_examples = False
    clause = "given"
    cases: List[SpecCase] = []

    def Finish() -> None:
        nonlocal current
        if current is not None:
            cases.append(Build(current, feature_name, path, digest, where))
            current = None

    for number, raw in enumerate(text.splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue

        if line.startswith("@"):
            pending_tags = [tag.lstrip("@") for tag in line.split()]
            continue

        keyword = KEYWORD.match(line)
        if keyword:
            word = keyword.group(1).lower()
            title = keyword.group(2).strip()
            in_examples = False
            in_background = False
            clause = "given"

            if word == "feature":
                Finish()
                feature_name = title
                background = Draft()
                outline = None
            elif word == "background":
                Finish()
                background = Draft()
                outline = None
                in_background = True
            elif word in ("scenario outline", "scenario template"):
                Finish()
                outline = background.Copy()
                outline.name = title
                outline.line = number
                outline.tags = list(pending_tags)
                # An outline is only ever realised through its Examples rows.
                current = None
                pending_tags = []
            elif word == "scenario":
                Finish()
                current = background.Copy()
                current.name = title
                current.line = number
                current.tags = list(pending_tags)
                outline = None
                pending_tags = []
            else:  # Examples / Scenarios
                if outline is None:
                    raise GherkinError(f"{where}:{number}: Examples without a Scenario Outline")
                Finish()
                in_examples = True
                examples_header = []
            continue

        if in_examples:
            if not line.startswith("|"):
                raise GherkinError(f"{where}:{number}: expected an Examples table row")
            cells = [cell.strip() for cell in line.strip("|").split("|")]
            if not examples_header:
                examples_header = cells
                continue
            if len(cells) != len(examples_header):
                raise GherkinError(
                    f"{where}:{number}: Examples row has {len(cells)} cell(s), "
                    f"the header has {len(examples_header)}")
            assert outline is not None
            bindings = dict(zip(examples_header, cells))
            cases.append(Build(Expand(outline, bindings, where, number),
                               feature_name, path, digest, where))
            continue

        step = STEP.match(line)
        if not step:
            raise GherkinError(f"{where}:{number}: expected a Given/When/Then step, got {line!r}")

        word = step.group(1).lower()
        if word in ("given", "when", "then"):
            clause = word
        # And / But / * continue the clause above them, as Gherkin defines.

        target = background if in_background else (outline if outline is not None else current)
        if target is None:
            raise GherkinError(f"{where}:{number}: step outside a Scenario: {line!r}")

        target.steps.append((clause, step.group(2), number))

    Finish()

    if not cases:
        raise GherkinError(f"{where}: no scenarios found")
    return cases


def Expand(outline: Draft, bindings: Dict[str, str], where: str, number: int) -> Draft:
    """One row of an Examples table, as a concrete scenario."""
    draft = outline.Copy()
    draft.steps = [(clause, Substitute(text, bindings, where, line), line)
                   for clause, text, line in outline.steps]
    label = ", ".join(f"{key}={value}" for key, value in bindings.items())
    draft.name = f"{outline.name} [{label}]"
    draft.line = number
    return draft


def Substitute(text: str, bindings: Dict[str, str], where: str, number: int) -> str:
    def replace(match: "re.Match[str]") -> str:
        key = match.group(1)
        if key not in bindings:
            raise GherkinError(
                f"{where}:{number}: <{key}> has no column in the Examples table")
        return bindings[key]
    return PLACEHOLDER.sub(replace, text)


def Build(draft: Draft, feature: str, path: str, digest: str, where: str) -> SpecCase:
    """Compile a drafted scenario's steps and assemble the case."""
    scenario = ""
    heroes: List[str] = []
    seed = 1
    expert = False
    given: List[GivenStep] = []
    when: List[WhenStep] = []
    then: List[ThenStep] = []

    for clause, text, number in draft.steps:
        if PLACEHOLDER.search(text):
            raise GherkinError(
                f"{where}:{number}: {text!r} still has a <placeholder>; "
                f"a Scenario Outline needs an Examples table")

        compiled = CompileStep(clause, text)
        if compiled is None:
            raise GherkinError(
                f"{where}:{number}: no {clause.title()} step matches {text!r}. "
                f"See docs/spec-harness.md for the step vocabulary.")

        try:
            kind = compiled[0]
            if kind == "setting":
                key, value = compiled[1], compiled[2]
                if key == "scenario":
                    scenario = str(value)
                elif key == "hero":
                    heroes = [str(value)]
                elif key == "heroes":
                    heroes = [str(x) for x in value]
                elif key == "seed":
                    seed = int(value)
                elif key == "expert":
                    expert = bool(value)
            elif kind == "given":
                given.append(compiled[1])
            elif kind == "when":
                when.append(compiled[1])
            else:
                then.append(compiled[1])
        except SpecCaseError as exc:
            raise GherkinError(f"{where}:{number}: {exc}") from exc

    try:
        return SpecCase(
            name=draft.name,
            feature=feature,
            scenario=scenario,
            heroes=tuple(heroes),
            seed=seed,
            expert=expert,
            tags=tuple(draft.tags),
            given=tuple(given),
            when=tuple(when),
            then=tuple(then),
            source_path=path,
            source_sha256=digest,
        )
    except SpecCaseError as exc:
        raise GherkinError(f"{where}:{draft.line}: {exc}") from exc


################################################################################
#

def LoadFeatureFile(path: str) -> List[SpecCase]:
    with open(path, "r", encoding="utf-8") as handle:
        return ParseFeature(handle.read(), path=NormalisePath(path))


def NormalisePath(path: str) -> str:
    """One spelling per file, so a manifest does not churn on how it was invoked.

    `validate specs/scenarios` and `validate ./specs/scenarios` must record the
    same source path, or every run would rewrite `trusted.json`.
    """
    text = path.replace(os.sep, "/")
    while text.startswith("./"):
        text = text[2:]
    return text
