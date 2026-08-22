"""Which cards have behavioral specs, at what depth, and what is left.

    python -m tools.spec.coverage                    # the summary
    python -m tools.spec.coverage --tier interactive # what to author next
    python -m tools.spec.coverage --pack core
    python -m tools.spec.coverage --rulings          # printed text is not the last word
    python -m tools.spec.coverage --out coverage.json

MARVEL-68's fourth acceptance item. `tools/coverage/report.py` answers "which
cards did the *corpus* exercise"; this answers "which cards has somebody written
down a claim about", and only the second survives the Python engine.

Run from `py_src/`.

## Coverage means a trusted scenario, not a scenario

A card counts as covered when at least one scenario tagged `@card:<id>` is in
`specs/trusted.json`. A quarantined scenario is reported separately and never as
coverage: it is a claim that failed, so counting it would mean the number goes up
when authoring goes wrong.

## Depth comes from the script, not from a quota

"Three scenarios per card" is the wrong rule -- it over-serves 602 declarative
scripts and under-serves the 493 that stop mid-resolution to ask a question.
`datasets/cards/cards.json` carries `engine.script` per card, built by
`tools/cards/extract.py`, and the tier is read from it:

    interactive   calls PlayerAsk / ChooseAbilities / MayChooseOneAbility /
                  AskSpendResources, so the card asks the player a question --
                  or reaches one of those through a `game/operate/` helper that
                  cannot avoid asking (MARVEL-114)
    imperative    a handler that does something, but never suspends
    declarative   a script with no function defined inside another, so no
                  branch a scenario could take differently
    stats_only    no script, but the engine has the card -- printed stats and
                  keywords, implemented generically rather than per card
    absent        the engine has no such card, or nothing to assert about it

The budget attached to each tier is a target for planning, not a gate. A card
that needs four scenarios gets four; the tier says which cards deserve the
argument.

**`stats_only` is specifiable and it took a bug to notice.** The first version of
this tier rule read "no script" as "nothing to specify" and put 563 cards outside
the denominator. Two of them were Hydra Mercenary and Sandman -- whose Guard and
Toughness `specs/rules/keywords.feature` already pins, because those keywords are
implemented by `game/card/face/attribute/` for every card that prints them rather
than by a script per card. A card with no script still has hit points, an attack
value and its keywords. What is genuinely unspecifiable is `absent`: 345 cards
in the printed dataset that the engine does not implement at all.

That distinction moves the denominator from 3781 to 3996, and getting it wrong
in the other direction is the MARVEL-16 failure repeated -- a missed population
does not look like a bug, it looks like a smaller universe.

## `RULING` marks a card the printed words do not settle

A card carrying an official MarvelCDB ruling is flagged `RULING` in the work
lists, and `--rulings` narrows to those cards (MARVEL-143). This is not coverage
and never counts as any: it is a warning about the *input*.

An author reading only printed text on such a card writes their reading, has it
validated against the Python engine -- which implements the same reading -- and
the scenario passes into `trusted.json` having confirmed nothing. Read
`python -m tools.cards.rulings <card_id>` first.

The flag is absent, harmlessly, on a clone that has not harvested
`datasets/marvelcdb-faq/`. See that dataset's UPSTREAM.md.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

from tools.cards import rulings as rulings_module

CARD_DATASET = "../datasets/cards/cards.json"
SPEC_ROOT = "specs"
TRUSTED = "specs/trusted.json"
QUARANTINE = "specs/quarantine.json"
UNREACHABLE = "specs/unreachable.json"

# tier -> (scenarios a card of this tier is planned for, why)
TIERS: Dict[str, Tuple[int, str]] = {
    "interactive": (4, "suspends mid-resolution for a player answer"),
    "imperative":  (2, "a handler that does something, but never asks"),
    "declarative": (1, "declarative factory calls, no branch to take"),
    "stats_only":  (1, "no script; printed stats and keywords only"),
    "absent":      (0, "the engine has no such card"),
}

TIER_ORDER: Tuple[str, ...] = (
    "interactive", "imperative", "declarative", "stats_only", "absent")

# Everything except `absent`. The denominator of the campaign.
SPECIFIABLE: Tuple[str, ...] = TIER_ORDER[:-1]


def Tier(card: Dict[str, Any]) -> str:
    """Which depth tier this card sits in.

    Script shape first, because that is what decides how much a card can do.
    A card with no script is not automatically out of scope: the engine
    implements keywords and stats generically, so a scripted Hydra Mercenary is
    not needed for its Guard to be worth pinning.
    """
    engine = card.get("engine") or {}
    script = engine.get("script")
    if script:
        # Either kind of evidence. `player_choice_calls` is a prompt the script
        # names; `player_choice_helpers` is one it reaches through a
        # `game/operate/` helper that cannot avoid asking. Reading only the
        # first put cards that stop and ask into the `imperative` list, under a
        # description -- "never suspends" -- that was flatly false for them
        # (MARVEL-114).
        if script.get("player_choice_calls") or script.get("player_choice_helpers"):
            return "interactive"
        if script.get("has_imperative_handler"):
            return "imperative"
        return "declarative"
    # No script. The engine either has the card or it does not, and only the
    # second is out of scope -- a scenario cannot name a card the engine has
    # never heard of.
    if card.get("in_engine") and engine.get("attributes"):
        return "stats_only"
    return "absent"


################################################################################
#


def ReadJson(path: str) -> Dict[str, Any]:
    with open(path, "r", encoding="utf-8") as handle:
        return json.load(handle)


def Cards(path: str = CARD_DATASET) -> List[Dict[str, Any]]:
    if not os.path.exists(path):
        raise SystemExit(f"{path}: not found -- run from py_src/")
    return list(ReadJson(path)["cards"])


def Manifest(path: str) -> Dict[str, Any]:
    """A validation-runner manifest, or empty if it has never been written."""
    if not os.path.exists(path):
        return {}
    return dict(ReadJson(path).get("scenarios") or {})


def Unreachable(path: str = UNREACHABLE) -> List[Dict[str, Any]]:
    """The recorded dispositions: decision paths no scenario can reach.

    Hand-authored, unlike every other file this module reads. `trusted.json`
    and `quarantine.json` are written by the validation runner from what it
    observed; nothing observes "the vocabulary has no way to say this", so
    somebody has to write it down.

    It exists because the alternative was tried and does not work. Three core
    spec files carried a gap like this as prose in their header -- 01040b's
    Foresight, 01116a's encounter-deck search, 01096's expert-only stage -- and
    `--pack core` counted all three as covered and reported nothing missing.
    That is the MARVEL-16 failure shape: a missed population does not look like
    a bug, it looks like a smaller universe.

    **An entry is a debt, not a discount.** It never raises `covered`, never
    lowers a tier's plan, and never turns a `--shallow` row green. All it does
    is refuse to disappear.
    """
    if not os.path.exists(path):
        return []
    return list(ReadJson(path).get("unreachable") or [])


def TaggedCards(root: str = SPEC_ROOT) -> Dict[str, List[str]]:
    """case_id -> the card ids its `@card:` tags name.

    The manifests key by `case_id` and carry no tags, so the tags have to come
    from the scenarios themselves. Parsing them is also the only way to notice a
    scenario that names a card id no card has.
    """
    from tools.spec.run_case import LoadCases

    tagged: Dict[str, List[str]] = {}
    for case in LoadCases(root):
        if case.card_tags:
            tagged[case.case_id] = list(case.card_tags)
    return tagged


################################################################################
#


class Coverage:
    """The join: every card, its tier, and the scenarios claiming it."""

    def __init__(self, cards: Sequence[Dict[str, Any]],
                 tagged: Dict[str, List[str]],
                 trusted: Iterable[str], quarantined: Iterable[str],
                 unreachable: Sequence[Dict[str, Any]] = (),
                 rulings: Optional[Dict[str, List[Any]]] = None) -> None:
        self.cards = {card["card_id"]: card for card in cards}
        self.tier = {card_id: Tier(card) for card_id, card in self.cards.items()}

        # card_id -> the official rulings MarvelCDB records against it
        # (MARVEL-143). Empty when nobody has harvested the snapshot, which is
        # a normal state for a fresh clone and never an error: a ruling is an
        # input to authoring, not something coverage is measured against.
        self.rulings: Dict[str, List[Any]] = dict(rulings or {})

        # card_id -> the recorded dispositions against it. A card can have more
        # than one unreachable path, and each is its own debt.
        self.unreachable: Dict[str, List[Dict[str, Any]]] = collections.defaultdict(list)
        for entry in unreachable:
            self.unreachable[str(entry.get("card") or "")].append(dict(entry))

        trusted, quarantined = set(trusted), set(quarantined)
        self.trusted: Dict[str, List[str]] = collections.defaultdict(list)
        self.quarantined: Dict[str, List[str]] = collections.defaultdict(list)
        # A tag naming a card the dataset does not have is a typo in a scenario,
        # not a covered card. Reported rather than counted.
        self.unknown_tags: Dict[str, List[str]] = collections.defaultdict(list)

        for case_id, card_ids in tagged.items():
            for card_id in card_ids:
                if card_id not in self.cards:
                    self.unknown_tags[case_id].append(card_id)
                elif case_id in trusted:
                    self.trusted[card_id].append(case_id)
                elif case_id in quarantined:
                    self.quarantined[card_id].append(case_id)

        self.credited_to = self.Equivalents()

    def ScriptPath(self, card_id: str) -> str:
        card = self.cards.get(card_id) or {}
        return str(((card.get("engine") or {}).get("script") or {}).get("path")
                   or "")

    def Identity(self, card_id: str) -> Tuple[str, str]:
        """What a scenario about this card would actually be asserting.

        Printed text and the statistics the engine gives it. Two cards agreeing
        on both, and running the same code, cannot disagree about anything a
        scenario can say.

        The dataset's own `stats` block is deliberately *not* compared. It
        distinguishes exactly one group in 3,996 -- the `cap` printing of Hail
        Hydra! carries an empty `stats` while the two `trors` printings record
        `boost: 2` -- and all three agree on `Boost: "2"` in the engine
        attributes, which is what the engine actually reads. Comparing it would
        refuse a credit over a hole in the metadata rather than a difference in
        behaviour.
        """
        card = self.cards.get(card_id) or {}
        return (
            str(card.get("text_plain") or ""),
            json.dumps((card.get("engine") or {}).get("attributes") or {},
                       sort_keys=True),
        )

    def Equivalents(self) -> Dict[str, str]:
        """card_id -> the id whose scenarios also cover it, where one exists.

        Two rules, because the evidence differs (MARVEL-105).

        **A shared script module.** 226 card ids run a module another card
        already runs while printing identical text, identical engine attributes
        and identical stats. Same code and same card means a scenario cannot
        distinguish them, so it is one claim written twice. This is the rule
        that catches "Chaos In the Prison" at 07011, 07026 and 07056 -- three
        ids, one card, no `reprint_of` link between them.

        The statistics are load-bearing here and were nearly left out. 34
        same-text, same-module groups are **villain stages**: same ability text,
        same script, different HP, ATK and SCH. A scenario asserting hit points
        does not transfer from stage 1 to stage 2, so text and code alone are
        not enough to credit on.

        **A `reprint_of` link, for cards with no script at all.** 129 of these,
        `stats_only` cards whose behaviour is printed keywords the engine
        applies from `game/card/face/attribute/`. The link is doing real work:
        without a module to compare, the structural rule alone would credit any
        two scriptless cards agreeing on text and stats, and 44 unrelated cards
        share the text "Max 1 per deck." with an identical stat block.

        Never credited either way: the 10 reprints that run a script file of
        their own. No pair is byte-identical and six of the ten disagree in
        behaviour (MARVEL-106) -- the one group where two ids provably do
        different things.
        """
        credited: Dict[str, str] = {}

        groups: Dict[Any, List[str]] = collections.defaultdict(list)
        for card_id in self.cards:
            path = self.ScriptPath(card_id)
            if path:
                groups[(path,) + self.Identity(card_id)].append(card_id)
        for members in groups.values():
            if len(members) < 2:
                continue
            # Lowest id is the canonical one, so a group can never form a chain
            # and `Scenarios` needs only one hop.
            canonical, *rest = sorted(members)
            for card_id in rest:
                credited[card_id] = canonical

        for card_id, card in self.cards.items():
            if card_id in credited or self.ScriptPath(card_id):
                continue
            original = str(card.get("reprint_of") or "")
            if original not in self.cards or self.ScriptPath(original):
                continue
            if self.Identity(card_id) == self.Identity(original):
                credited[card_id] = original

        return credited

    def Specifiable(self) -> List[str]:
        """Cards a scenario could be written for -- everything the engine has."""
        return [card_id for card_id, tier in self.tier.items()
                if tier in SPECIFIABLE]

    def Scenarios(self, card_id: str) -> List[str]:
        """The scenarios covering this card, its original's included."""
        own = self.trusted.get(card_id, [])
        original = self.credited_to.get(card_id)
        return own + self.trusted.get(original, []) if original else own

    def Covered(self, card_id: str) -> bool:
        return bool(self.Scenarios(card_id))

    def AtDepth(self, card_id: str) -> bool:
        """Covered to the depth its tier plans for, not merely covered.

        MARVEL-87. `Covered` credits a card for one trusted scenario whatever
        its tier asks. That is the right rule for "has anyone looked at this
        card", and the wrong one for "is this card done": an interactive card
        is planned for four scenarios because it has four decision paths, and
        one scenario covering one of them is a quarter of the job reported as
        the whole of it.

        It matters most under delegation, which is what MARVEL-87 measured. 479
        thin interactive scenarios would report that tier fully covered against
        a plan of 1,916, and nothing in the numbers would say otherwise.

        The plan is a target, not a gate -- `spec-campaign.md` is explicit that
        a card needing four gets four whatever its tier says. So this is
        reported beside `covered`, not instead of it.
        """
        planned = TIERS[self.tier.get(card_id, "")][0] if card_id in self.tier else 0
        return len(self.Scenarios(card_id)) >= planned > 0

    def ByTier(self) -> Dict[str, Dict[str, int]]:
        rows: Dict[str, Dict[str, int]] = {
            tier: {"cards": 0, "covered": 0, "at_depth": 0, "scenarios": 0,
                   "quarantined": 0, "planned": 0}
            for tier in TIER_ORDER}
        for card_id, tier in self.tier.items():
            row = rows[tier]
            row["cards"] += 1
            row["planned"] += TIERS[tier][0]
            row["scenarios"] += len(self.trusted.get(card_id, ()))
            row["quarantined"] += len(self.quarantined.get(card_id, ()))
            if self.Covered(card_id):
                row["covered"] += 1
            if self.AtDepth(card_id):
                row["at_depth"] += 1
        return rows

    def ByPack(self) -> Dict[str, Dict[str, int]]:
        rows: Dict[str, Dict[str, int]] = collections.defaultdict(
            lambda: {"cards": 0, "covered": 0})
        for card_id, tier in self.tier.items():
            if tier not in SPECIFIABLE:
                continue
            pack = str(self.cards[card_id].get("pack") or "?")
            rows[pack]["cards"] += 1
            if self.Covered(card_id):
                rows[pack]["covered"] += 1
        return dict(rows)

    def Uncovered(self, tier: str = "", pack: str = "",
                  rulings: bool = False) -> List[Dict[str, Any]]:
        """Specifiable cards with no trusted scenario, worst tier first.

        Ordered by tier and then by script size, so the head of the list is
        where a scenario buys the most. Not by card id -- that would walk the
        core set to exhaustion before touching anything else, and the tail is
        where port bugs hide.

        `rulings=True` narrows to cards an official ruling exists for. Those are
        the cards where authoring from the printed words alone is most likely to
        be confidently wrong, so they are worth writing while the ruling is in
        front of you rather than in card-id order (MARVEL-143).
        """
        rows: List[Dict[str, Any]] = []
        for card_id in self.Specifiable():
            if self.Covered(card_id):
                continue
            card = self.cards[card_id]
            card_tier = self.tier[card_id]
            if tier and card_tier != tier:
                continue
            if pack and str(card.get("pack") or "") != pack:
                continue
            if rulings and not self.rulings.get(card_id):
                continue
            script = (card.get("engine") or {}).get("script") or {}
            rows.append({
                "card_id": card_id,
                "name": card.get("name") or "",
                "type": card.get("type_name") or "",
                "pack": card.get("pack") or "",
                "tier": card_tier,
                "lines": int(script.get("lines") or 0),
                "asks": list(script.get("player_choice_calls") or ()),
                "via": list(script.get("player_choice_helpers") or ()),
                "quarantined": len(self.quarantined.get(card_id, ())),
                "rulings": len(self.rulings.get(card_id, ())),
            })
        rows.sort(key=lambda r: (TIER_ORDER.index(r["tier"]), -r["lines"],
                                 r["card_id"]))
        return rows

    def Shallow(self, tier: str = "", pack: str = "",
                rulings: bool = False) -> List[Dict[str, Any]]:
        """Covered cards short of their tier's plan, biggest shortfall first.

        `Uncovered` answers "what has nobody looked at". This answers the other
        half of the same question -- what got looked at once and reported as
        done. The tier table has printed that gap as two numbers since
        MARVEL-87, with no way to list what is in it, so the only thing anyone
        could act on was the total.

        That is the wrong way round for a delegated campaign. An uncovered card
        announces itself; a card sitting at one scenario of four does not, and
        it is the cheaper of the two to finish because whoever wrote the first
        scenario already read the script.
        """
        rows: List[Dict[str, Any]] = []
        for card_id in self.Specifiable():
            if not self.Covered(card_id) or self.AtDepth(card_id):
                continue
            card = self.cards[card_id]
            card_tier = self.tier[card_id]
            if tier and card_tier != tier:
                continue
            if pack and str(card.get("pack") or "") != pack:
                continue
            if rulings and not self.rulings.get(card_id):
                continue
            script = (card.get("engine") or {}).get("script") or {}
            have = len(self.Scenarios(card_id))
            planned = TIERS[card_tier][0]
            rows.append({
                "card_id": card_id,
                "name": card.get("name") or "",
                "type": card.get("type_name") or "",
                "pack": card.get("pack") or "",
                "tier": card_tier,
                "lines": int(script.get("lines") or 0),
                "asks": list(script.get("player_choice_calls") or ()),
                "via": list(script.get("player_choice_helpers") or ()),
                "scenarios": have,
                "planned": planned,
                "short": planned - have,
                "quarantined": len(self.quarantined.get(card_id, ())),
                "rulings": len(self.rulings.get(card_id, ())),
            })
        rows.sort(key=lambda r: (TIER_ORDER.index(r["tier"]), -r["short"],
                                 r["card_id"]))
        return rows

    def UnreachableRows(self, pack: str = "") -> List[Dict[str, Any]]:
        """The recorded dispositions, optionally for one pack.

        An entry naming a card the dataset does not have is kept and flagged
        rather than dropped. A record that silently forgets its own stale
        entries is the failure it exists to prevent, one level up.
        """
        rows: List[Dict[str, Any]] = []
        for card_id, entries in sorted(self.unreachable.items()):
            card = self.cards.get(card_id)
            card_pack = str((card or {}).get("pack") or "")
            if pack and card_pack != pack:
                continue
            for entry in entries:
                rows.append({
                    "card_id": card_id,
                    "name": (card or {}).get("name") or "",
                    "pack": card_pack,
                    "tier": self.tier.get(card_id, ""),
                    "known_card": card is not None,
                    "covered": self.Covered(card_id),
                    "path": entry.get("path") or "",
                    "why": entry.get("why") or "",
                    "blocked_by": entry.get("blocked_by") or "",
                    "feature": entry.get("feature") or "",
                    "issue": entry.get("issue") or "",
                })
        return rows

    def Done(self, pack: str) -> Tuple[bool, List[str]]:
        """Whether a shard may be called done, and what is in the way.

        `spec-campaign.md` defines done as every card in the pack having a
        scenario. That definition was checkable only against the uncovered
        list, which a card with a covered-but-incomplete decision path drops
        straight out of -- so a shard could be reported complete over a hole
        nobody could see. A recorded disposition is the hole made visible, and
        it counts against done until somebody deletes it.
        """
        reasons: List[str] = []
        uncovered = self.Uncovered(pack=pack)
        if uncovered:
            reasons.append(f"{len(uncovered)} uncovered card(s)")
        shallow = self.Shallow(pack=pack)
        if shallow:
            reasons.append(f"{len(shallow)} card(s) short of their tier's plan")
        recorded = self.UnreachableRows(pack=pack)
        if recorded:
            reasons.append(
                f"{len(recorded)} recorded unreachable decision path(s)")
        return (not reasons), reasons

    def DuplicateSummary(self) -> Dict[str, Any]:
        """The reprint join, over specifiable cards only.

        Scoped deliberately. 13 more cards reprint a card the engine *does*
        implement while having no script of their own, so they tier as `absent`
        and sit outside the campaign entirely. Listing them beside the ones that
        run a rival implementation would read as though the engine disagreed
        with itself about them, when what it does is not have them at all.
        """
        specifiable = set(self.Specifiable())
        credited = [c for c in self.credited_to if c in specifiable]
        return {
            "credited": len(credited),
            "credited_and_covered": sum(1 for c in credited if self.Covered(c)),
            "not_credited": sorted(
                card_id for card_id in specifiable
                if str(self.cards[card_id].get("reprint_of") or "") in self.cards
                and card_id not in self.credited_to),
        }

    def ToDict(self) -> Dict[str, Any]:
        specifiable = self.Specifiable()
        covered = [card_id for card_id in specifiable if self.Covered(card_id)]
        at_depth = [card_id for card_id in specifiable if self.AtDepth(card_id)]
        return {
            "note": ("Coverage is a card with at least one scenario in "
                     "specs/trusted.json tagged @card:<id>. A quarantined "
                     "scenario is a claim that failed and is never coverage."),
            "totals": {
                "cards": len(self.cards),
                "specifiable": len(specifiable),
                "covered": len(covered),
                "at_depth": len(at_depth),
                "scenarios": sum(len(v) for v in self.trusted.values()),
                "quarantined": sum(len(v) for v in self.quarantined.values()),
                # Cards, not rulings: one card can carry several Q&As in a
                # single FAQ entry, and what an author picks from is the card.
                "with_ruling": sum(1 for c in specifiable if self.rulings.get(c)),
                "uncovered_with_ruling": sum(
                    1 for c in specifiable
                    if self.rulings.get(c) and not self.Covered(c)),
                # Paths, not cards: one card can hold more than one.
                "unreachable": sum(len(v) for v in self.unreachable.values()),
            },
            "unreachable": self.UnreachableRows(),
            "duplicates": self.DuplicateSummary(),
            "by_tier": self.ByTier(),
            "by_pack": self.ByPack(),
            "unknown_tags": {k: v for k, v in self.unknown_tags.items()},
        }


################################################################################
#


def Rulings(cards: Sequence[Dict[str, Any]]) -> Dict[str, List[Any]]:
    """Official rulings by card id, or nothing when none have been harvested.

    `datasets/marvelcdb-faq/` is vendored by a manual harvest, so a clone that
    has not run one does not have it. That is not an error and must not stop
    this tool: coverage is measured against scenarios, and a ruling only changes
    what an author reads before writing one.

    A snapshot that is present but malformed *does* raise, from
    `tools.cards.rulings.Load`. A corrupted checked-in dataset silently reporting
    "no rulings" is the failure worth being loud about, because it looks exactly
    like the normal empty state.
    """
    data = rulings_module.Load()
    if not data.Loaded():
        return {}
    return rulings_module.ByCard(data, {card["card_id"] for card in cards})


def Build() -> Coverage:
    cards = Cards()
    return Coverage(
        cards=cards,
        tagged=TaggedCards(),
        trusted=Manifest(TRUSTED),
        quarantined=Manifest(QUARANTINE),
        unreachable=Unreachable(),
        rulings=Rulings(cards),
    )


def _Asks(row: Dict[str, Any]) -> str:
    """What this card stops to ask, and whether it does so through a helper.

    The helper is worth printing rather than flattening into `asks`. An author
    walking `--tier interactive` needs to know that 40018's question is asked by
    `Search.PlayerCard` and is not written anywhere in the card's own script.
    """
    parts = list(row.get("asks") or ())
    parts += [f"via {name}" for name in (row.get("via") or ())]
    return f"  asks: {', '.join(parts)}" if parts else ""


def Percent(part: int, whole: int) -> str:
    return f"{100.0 * part / whole:.1f}%" if whole else "n/a"


def _Flags(row: Dict[str, Any]) -> str:
    """The markers that hang off the end of a work-list row.

    `RULING` is not decoration. It says an official ruling exists for this card,
    so the printed words are not the last word on it -- read
    `python -m tools.cards.rulings <id>` before asserting anything about timing.
    """
    flags = "  QUARANTINED" if row.get("quarantined") else ""
    return flags + ("  RULING" if row.get("rulings") else "")


def Report(coverage: Coverage, tier: str, pack: str, top: int,
           shallow: bool = False, rulings: bool = False) -> None:
    data = coverage.ToDict()
    totals = data["totals"]

    print(f"cards             {totals['cards']}")
    print(f"specifiable       {totals['specifiable']} (the engine has the card)")
    print(f"covered           {totals['covered']} "
          f"({Percent(totals['covered'], totals['specifiable'])})")
    print(f"trusted scenarios {totals['scenarios']}")
    duplicates = data["duplicates"]
    if duplicates["credited"]:
        print(f"  of which credited {duplicates['credited_and_covered']}, "
              f"covered by another id printing the same card, of "
              f"{duplicates['credited']} that do")
    if duplicates["not_credited"]:
        print(f"  not credited      {len(duplicates['not_credited'])} reprint(s) "
              f"that run a script of their own (MARVEL-106):"
              f"\n                    "
              f"{', '.join(duplicates['not_credited'])}")
    if totals["quarantined"]:
        print(f"quarantined       {totals['quarantined']} "
              f"(claims that failed -- not coverage)")
    if totals["with_ruling"]:
        print(f"with a ruling     {totals['with_ruling']} card(s), of which "
              f"{totals['uncovered_with_ruling']} uncovered -- printed text is "
              f"\n                  not the last word on these "
              f"(`--rulings` lists them)")
    if totals["unreachable"]:
        print(f"unreachable       {totals['unreachable']} decision path(s) "
              f"recorded in {UNREACHABLE}")
    print()

    print(f"{'tier':<13} {'cards':>6} {'covered':>8} {'at depth':>9} "
          f"{'scenarios':>10} {'planned':>8}  what it means")
    for name in TIER_ORDER:
        row = data["by_tier"][name]
        if not row["cards"]:
            continue
        print(f"{name:<13} {row['cards']:>6} "
              f"{row['covered']:>8} {row['at_depth']:>9} {row['scenarios']:>10} "
              f"{row['planned']:>8}  {TIERS[name][1]}")
    print()
    print("'covered' is a card with any trusted scenario; 'at depth' is one with"
          "\nas many as its tier plans for. The gap between them is how much of"
          "\nthe covered set is one scenario deep -- which is what mass"
          "\ndelegation produces if nothing watches for it (MARVEL-87).")
    print()

    if coverage.unknown_tags:
        print("scenarios tagging a card id the dataset does not have:")
        for case_id, ids in sorted(coverage.unknown_tags.items()):
            print(f"  {case_id}: {', '.join(ids)}")
        print()

    label = " ".join(x for x in (tier, pack) if x) or "all tiers"

    # Before the work list, not after it. A recorded disposition is a card that
    # already has scenarios, so it is in neither the uncovered nor -- once its
    # reachable paths are written -- the shallow list, and printing it last
    # would let "nothing uncovered matches that filter" be the last word.
    recorded = coverage.UnreachableRows(pack=pack)
    if recorded:
        print(f"unreachable decision paths ({pack or 'all packs'}) -- "
              f"{len(recorded)} recorded in {UNREACHABLE}.")
        print("These do not count as coverage and do not go away on their own;"
              "\na pack holding one is not done. Delete the entry when the"
              "\nvocabulary reaches the path.")
        for row in recorded:
            stale = "" if row["known_card"] else "  NO SUCH CARD"
            print(f"  {row['card_id']:<8} {row['pack']:<10} "
                  f"{row['name'][:28]:<28} {row['path']}{stale}")
            if row["blocked_by"]:
                print(f"           blocked by: {row['blocked_by']}"
                      + (f"  ({row['issue']})" if row["issue"] else ""))
        print()

    if rulings:
        label += " with a ruling"

    if shallow:
        rows = coverage.Shallow(tier=tier, pack=pack, rulings=rulings)
        if not rows:
            print("nothing covered matches that filter is short of its plan")
            return
        print(f"short of plan ({label}) -- {len(rows)} covered but not at depth,"
              f"\nbiggest shortfall first:")
        for row in rows[:top]:
            print(f"  {row['card_id']:<8} {row['tier']:<12} "
                  f"{row['scenarios']}/{row['planned']:<6} "
                  f"{row['pack']:<10} {row['name'][:34]:<34}"
                  f"{_Asks(row)}{_Flags(row)}")
        if len(rows) > top:
            print(f"  ... {len(rows) - top} more")
        return

    rows = coverage.Uncovered(tier=tier, pack=pack, rulings=rulings)
    if not rows:
        print("nothing uncovered matches that filter")
        return

    print(f"next up ({label}) -- {len(rows)} uncovered, "
          f"largest script first:")
    for row in rows[:top]:
        print(f"  {row['card_id']:<8} {row['tier']:<12} {row['lines']:>4}L "
              f"{row['pack']:<10} {row['name'][:34]:<34}"
              f"{_Asks(row)}{_Flags(row)}")
    if len(rows) > top:
        print(f"  ... {len(rows) - top} more")


def main(argv: Optional[List[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__.split("\n\n")[0],
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--tier", default="", choices=("",) + TIER_ORDER,
                        help="only list uncovered cards in this depth tier")
    parser.add_argument("--pack", default="", help="only list this pack")
    parser.add_argument("--top", type=int, default=25,
                        help="how many uncovered cards to list (default 25)")
    parser.add_argument("--shallow", action="store_true",
                        help="list covered cards short of their tier's plan "
                             "instead of uncovered ones")
    parser.add_argument("--rulings", action="store_true",
                        help="only list cards an official MarvelCDB ruling "
                             "exists for -- where authoring from the printed "
                             "words alone is most likely to be wrong")
    parser.add_argument("--out", default="", help="write the full report as JSON")
    parser.add_argument("--done", default="", metavar="PACK",
                        help="verdict on whether a shard is finished; exits "
                             "non-zero while anything is in the way, including "
                             "a recorded unreachable decision path")
    args = parser.parse_args(argv)

    coverage = Build()

    # A machine-readable answer to the one question `spec-campaign.md` asks of
    # a shard. It is a separate flag rather than an exit code on `--pack`
    # because `--pack` is a work list somebody reads, and a work list that
    # exits 1 gets its exit code ignored.
    if args.done:
        done, reasons = coverage.Done(args.done)
        if done:
            print(f"{args.done}: done")
            return 0
        print(f"{args.done}: not done -- {'; '.join(reasons)}")
        for row in coverage.UnreachableRows(pack=args.done):
            print(f"  unreachable  {row['card_id']} {row['path']}"
                  f" -- {row['blocked_by'] or row['why']}")
        return 1

    Report(coverage, tier=args.tier, pack=args.pack, top=args.top,
           shallow=args.shallow, rulings=args.rulings)

    if args.out:
        payload = coverage.ToDict()
        payload["uncovered"] = coverage.Uncovered(tier=args.tier, pack=args.pack)
        payload["shallow"] = coverage.Shallow(tier=args.tier, pack=args.pack)
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump(payload, handle, indent=2, sort_keys=True)
            handle.write("\n")
        print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
