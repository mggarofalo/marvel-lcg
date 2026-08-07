"""The structured test-case format: setup commands, action, expected outcome.

A `SpecCase` is the intermediate representation every front end compiles to.
`tools/spec/gherkin.py` produces one from a `.feature` file; this module can
load and save the same thing as JSON so a case is inspectable and diffable
without a parser in the loop.

Three step kinds, matching the three clauses:

- `GivenStep` names a verb from `GIVEN_VERBS`. Every verb maps to a `RunPuzzle`
  method, and the mapping is a closed allowlist checked at load time -- an
  unknown verb is a load error, never a runtime surprise. Nothing here is
  `exec`-ed: the harness calls the bound method directly, so `PuzzleHelper.Exec`
  and its per-command `exec(f"c{c} = ...")` rebuild are out of the picture.
- `WhenStep` names an effect the way the client sees it, plus the card it is
  bound to when the name alone is ambiguous ("Play" is every card in hand).
- `ThenStep` is one assertion over readable state.

Cards are named by `CardRef` strings throughout -- see `tools/spec/resolve.py`.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, field
from typing import Any, Dict, List, Sequence, Tuple


class SpecCaseError(Exception):
    """A case that cannot be loaded. Raised at load time, not run time."""


################################################################################
# Given
#
# kind -> what the verb's arguments mean:
#   "create"            variadic card ids, generated into a zone
#   "card"              one card, a state flip with no magnitude
#   "card_value"        one card plus an integer
#   "card_named_value"  one card, a counter/token name, and an integer
#   "value"             just an integer

GIVEN_VERBS: Dict[str, Tuple[str, str]] = {
    # zone fills
    "hand":                 ("create", "CreateHandCards"),
    "player_deck":          ("create", "CreatePlayerDeck"),
    "player_discard":       ("create", "CreatePlayerDiscardPile"),
    "player_set_aside":     ("create", "CreatePlayerAdditionalDeck"),
    "encounter_deck":       ("create", "CreateEncounterDeck"),
    "encounter_discard":    ("create", "CreateEncounterDiscardPile"),
    # magnitudes
    "damage":               ("card_value", "Damage"),
    "heal":                 ("card_value", "Heal"),
    "threat":               ("card_value", "SetThreat"),
    "counters":             ("card_named_value", "Counter"),
    "tokens":               ("card_named_value", "Token"),
    # states
    "stunned":              ("card", "Stun"),
    "confused":             ("card", "Confuse"),
    "tough":                ("card", "Tough"),
    "exhausted":            ("card", "Exhaust"),
    "ready":                ("card", "Ready"),
    "discarded":            ("card", "Discard"),
    "in_play":              ("card", "PutIntoPlay"),
    "revealed":             ("card", "Reveal"),
    "hero_form":            ("card", "ChangeForm"),
    "alter_ego_form":       ("card", "ChangeForm"),
    # player
    "draw":                 ("value", "Draw"),
}

GIVEN_KIND = {verb: kind for verb, (kind, _) in GIVEN_VERBS.items()}


@dataclass(frozen=True)
class GivenStep:
    """One setup command. `verb` is a key of `GIVEN_VERBS`."""

    verb: str
    cards: Tuple[str, ...] = ()
    value: int = 0
    name: str = ""

    def __post_init__(self) -> None:
        if self.verb not in GIVEN_VERBS:
            known = ", ".join(sorted(GIVEN_VERBS))
            raise SpecCaseError(f"unknown Given verb {self.verb!r}; known verbs: {known}")

        kind = GIVEN_KIND[self.verb]
        if kind == "create" and not self.cards:
            raise SpecCaseError(f"Given {self.verb!r} needs at least one card")
        if kind in ("card", "card_value", "card_named_value") and len(self.cards) != 1:
            raise SpecCaseError(
                f"Given {self.verb!r} names exactly one card, got {len(self.cards)}")
        if kind == "card_named_value" and not self.name:
            raise SpecCaseError(f"Given {self.verb!r} needs a counter/token name")
        if kind == "value" and self.cards:
            raise SpecCaseError(f"Given {self.verb!r} takes no cards")

    def Describe(self) -> str:
        kind = GIVEN_KIND[self.verb]
        if kind == "create":
            return f"{self.verb} contains {', '.join(self.cards)}"
        if kind == "card":
            return f"{self.cards[0]} {self.verb}"
        if kind == "card_value":
            return f"{self.cards[0]} {self.verb} {self.value}"
        if kind == "card_named_value":
            return f"{self.cards[0]} has {self.value} {self.name!r} {self.verb}"
        return f"{self.verb} {self.value}"


################################################################################
# When

@dataclass(frozen=True)
class WhenStep:
    """One action, selected through the bot device.

    `option` is the effect name the client renders -- "Attack", "Thwart",
    "Play", "Change_Form", or a card's own ability name. `card` disambiguates
    when several options share a name, which is the normal case for "Play".
    `pass_priority` is the explicit "decline this decision" step; it is how a
    scenario says "end the turn" or "do not respond".
    """

    option: str = ""
    card: str = ""
    targets: Tuple[str, ...] = ()
    pass_priority: bool = False

    def __post_init__(self) -> None:
        if self.pass_priority:
            if self.option or self.card or self.targets:
                raise SpecCaseError("a pass step takes no option, card or targets")
        elif not self.option and not self.card:
            raise SpecCaseError("a When step needs an option name or a card")

    def Describe(self) -> str:
        if self.pass_priority:
            return "pass"
        text = self.option or "any option"
        if self.card:
            text += f" on {self.card}"
        if self.targets:
            text += f" targeting {', '.join(self.targets)}"
        return text


################################################################################
# Then

COMPARISONS = ("==", "!=", ">=", "<=", ">", "<")


@dataclass(frozen=True)
class ThenStep:
    """One assertion. `subject` is a `CardRef`, `player`/`player N`, or `game`."""

    subject: str
    prop: str
    value: Any
    op: str = "=="

    def __post_init__(self) -> None:
        if self.op not in COMPARISONS:
            raise SpecCaseError(
                f"unknown comparison {self.op!r}; use one of {', '.join(COMPARISONS)}")
        if not self.subject:
            raise SpecCaseError("a Then step needs a subject")
        if not self.prop:
            raise SpecCaseError("a Then step needs a property")

    def Describe(self) -> str:
        return f"{self.subject} {self.prop} {self.op} {Render(self.value)}"


def Render(value: Any) -> str:
    """Values as an author wrote them, not as Python repr()s them."""
    if isinstance(value, bool):
        return "true" if value else "false"
    if value is None:
        return "none"
    return str(value)


################################################################################
# Case

@dataclass(frozen=True)
class SpecCase:
    """One card behavior, executable against the engine."""

    name: str
    scenario: str
    heroes: Tuple[str, ...]
    given: Tuple[GivenStep, ...] = ()
    when: Tuple[WhenStep, ...] = ()
    then: Tuple[ThenStep, ...] = ()
    seed: int = 1
    expert: bool = False
    feature: str = ""
    # Provenance, used by the validation runner's quarantine (MARVEL-21).
    source_path: str = ""
    source_sha256: str = ""
    tags: Tuple[str, ...] = field(default=())

    def __post_init__(self) -> None:
        if not self.name:
            raise SpecCaseError("a case needs a name")
        if not self.scenario:
            raise SpecCaseError(f"case {self.name!r} needs a scenario")
        if not self.heroes:
            raise SpecCaseError(f"case {self.name!r} needs at least one hero")
        if not self.then:
            # An assertion-free case reports PASS while proving nothing.
            raise SpecCaseError(f"case {self.name!r} has no Then assertions")

    @property
    def case_id(self) -> str:
        """Stable identity across runs: the feature it came from plus its name."""
        if self.feature:
            return f"{self.feature} :: {self.name}"
        return self.name

    ############################################################################
    #
    def ToDict(self) -> Dict[str, Any]:
        return {
            "name": self.name,
            "feature": self.feature,
            "scenario": self.scenario,
            "heroes": list(self.heroes),
            "seed": self.seed,
            "expert": self.expert,
            "tags": list(self.tags),
            "source_path": self.source_path,
            "source_sha256": self.source_sha256,
            "given": [
                {"verb": s.verb, "cards": list(s.cards), "value": s.value, "name": s.name}
                for s in self.given
            ],
            "when": [
                {"option": s.option, "card": s.card, "targets": list(s.targets),
                 "pass_priority": s.pass_priority}
                for s in self.when
            ],
            "then": [
                {"subject": s.subject, "prop": s.prop, "op": s.op, "value": s.value}
                for s in self.then
            ],
        }

    def ToJson(self, *, indent: int | None = 2) -> str:
        return json.dumps(self.ToDict(), indent=indent, sort_keys=True)

    @staticmethod
    def FromDict(data: Dict[str, Any]) -> "SpecCase":
        try:
            return SpecCase(
                name=str(data["name"]),
                feature=str(data.get("feature", "")),
                scenario=str(data["scenario"]),
                heroes=tuple(str(x) for x in data["heroes"]),
                seed=int(data.get("seed", 1)),
                expert=bool(data.get("expert", False)),
                tags=tuple(str(x) for x in data.get("tags", ())),
                source_path=str(data.get("source_path", "")),
                source_sha256=str(data.get("source_sha256", "")),
                given=tuple(
                    GivenStep(
                        verb=str(s["verb"]),
                        cards=tuple(str(c) for c in s.get("cards", ())),
                        value=int(s.get("value", 0)),
                        name=str(s.get("name", "")),
                    )
                    for s in data.get("given", ())
                ),
                when=tuple(
                    WhenStep(
                        option=str(s.get("option", "")),
                        card=str(s.get("card", "")),
                        targets=tuple(str(t) for t in s.get("targets", ())),
                        pass_priority=bool(s.get("pass_priority", False)),
                    )
                    for s in data.get("when", ())
                ),
                then=tuple(
                    ThenStep(
                        subject=str(s["subject"]),
                        prop=str(s["prop"]),
                        op=str(s.get("op", "==")),
                        value=s["value"],
                    )
                    for s in data.get("then", ())
                ),
            )
        except KeyError as exc:
            raise SpecCaseError(f"case is missing required field {exc}") from exc

    @staticmethod
    def FromJson(text: str) -> "SpecCase":
        return SpecCase.FromDict(json.loads(text))


def LoadJsonCases(text: str, *, source_path: str = "") -> List[SpecCase]:
    """One case or a list of them, from JSON."""
    data = json.loads(text)
    items: Sequence[Any] = data if isinstance(data, list) else [data]
    digest = SourceDigest(text)
    cases: List[SpecCase] = []
    for item in items:
        item = dict(item)
        item.setdefault("source_path", source_path)
        item.setdefault("source_sha256", digest)
        cases.append(SpecCase.FromDict(item))
    return cases


def SourceDigest(text: str) -> str:
    """Content hash of a scenario source, newline-normalised.

    The validation runner keys the trusted suite on this: edit the scenario and
    it drops out of the trusted set on the next run, with no way to pin it.
    """
    normalised = text.replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(normalised.encode("utf-8")).hexdigest()
