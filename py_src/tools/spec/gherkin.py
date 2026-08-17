"""Authoring scenarios in Gherkin, compiled to `SpecCase`.

Scenarios are native `.feature` files consumed directly by both runners, because
the trusted suite has to outlive the Python engine. Reqnroll (the maintained
SpecFlow successor) binds the same step text to C# with `[Given(@"...")]`, so
the file that validates against Python today is the file the C# engine is held
to later. Nothing here depends on Reqnroll; what it depends on is that the step
text is a **closed vocabulary** rather than free prose.

That vocabulary lives in `specs/steps.catalogue.json`, checked in beside the
scenarios, and `unit_test/test_spec_validate.py` asserts this module implements
exactly it -- no extra forms, none missing. Step-definition drift then fails a
build instead of rotting silently.

A step that matches nothing is a parse error naming the file and line. A
scenario compiles completely or not at all, so a typo can never become a
silently skipped assertion.

Scenarios are written in the **first person, as a transcript**: one `When` per
decision, with `Then`s interleaved wherever the board is worth checking.

    Feature: Nick Fury

      Background:
        Given the scenario is "rhino"
        And the hero is "spider_man"

      @card:01084
      Scenario: damage is dealt to the chosen enemy, not the first one
        Given I am in hero form
        And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up"
        And "Shocker" is in play

        When I play "Nick Fury"
        Then I am prompted to choose one
          | Draw 3 cards              |
          | Deal 4 damage to an enemy |

        When I choose "Deal 4 damage to an enemy" targeting "Shocker"
        Then "Shocker" has 4 damage
        And I am not prompted again

Gherkin permits `When` after `Then`. One-`When`-per-scenario is a user-story BDD
convention, not a language constraint, and it is the wrong convention for an
interactive rules engine.

Supported: `Feature`, `Background`, `Scenario`, `Scenario Outline` with
`Examples`, `Given`/`When`/`Then`/`And`/`But`, `@tags`, `#` comments, and data
tables (used by the prompt assertion). Doc strings are not supported; a step
that needs a list takes a comma-separated one.
"""

from __future__ import annotations

import os
import re
from dataclasses import dataclass, field
from typing import Any, Callable, Dict, List, Optional, Pattern, Tuple

from tools.spec.case import (
    CannotStep, GivenStep, LimitStep, MinimumStep, NoPromptStep, NotOfferedStep,
    PromptStep, SourceDigest, SpecCase, TargetsStep,
    SpecCaseError, ThenStep, WhenStep)
from tools.spec.resolve import SplitRefs
from tools.spec.state import PHASE_NAMES


class GherkinError(SpecCaseError):
    """A `.feature` file that cannot be compiled, with the line that broke it."""


################################################################################
# Step vocabulary
#
# Each entry is (form, pattern, builder). `form` is the catalogue key: the step
# as an author writes it, with <placeholders>. A builder returns one of:
#   ("setting", key, value)   scenario-level configuration
#   ("given", GivenStep)
#   ("when", WhenStep)
#   ("then", ThenStep | PromptStep | NoPromptStep | "prompt" | ("targets", option))
#
# Quotes are required around card and option names so a card called
# "Hard to Keep Down" is unambiguous.

Q = r'"([^"]*)"'
N = r'(-?\d+)'
LIST = r'(.+)'

Entry = Tuple[str, Pattern[str], Callable[..., Any]]
Table = List[Entry]


def Rx(pattern: str) -> Pattern[str]:
    return re.compile(r"^" + pattern + r"$", re.IGNORECASE)


# One table per clause. They are kept apart because the same sentence means
# different things in different clauses: `"01097b" has 3 threat` sets up the
# board under Given and asserts it under Then. Gherkin's own keywords are the
# only thing that can tell them apart, so the parser uses them.

GIVEN_TABLE: Table = [

    # -- scenario configuration -------------------------------------------
    ('the scenario is "<name>"',
     Rx(r'the scenario is ' + Q),
     lambda name: ("setting", "scenario", name)),
    ('the hero is "<name>"',
     Rx(r'the hero is ' + Q),
     lambda name: ("setting", "hero", name)),
    ('the heroes are "<a>", "<b>"',
     Rx(r'the heroes are ' + LIST),
     lambda names: ("setting", "heroes", SplitRefs(names))),
    ('the seed is <n>',
     Rx(r'the seed is ' + N),
     lambda value: ("setting", "seed", int(value))),
    # Flips `campaign.expert`, which is what `Worlds.IsExpert` and every
    # `expert_mode_only` ability read. It does **not** swap the villain deck,
    # and it is not how a real game reaches expert content: standard and expert
    # are two scenario files with different villain stages and different
    # encounter sets, so the spelling for "play the expert scenario" is
    # `the scenario is "rhino_expert"`. See spec-harness.md, "Expert is a
    # scenario, not a flag".
    ('the difficulty is expert',
     Rx(r'the difficulty is expert'),
     lambda: ("setting", "expert", True)),

    # -- decks that exist before GameSetup() -------------------------------
    # A setup ability fires inside `GameSetup()`, before any `Given` has run,
    # so a deck it searches has to be part of the scene rather than stacked
    # afterwards. Shuffled at setup like a real game's, so unlike `my deck is`
    # these do not pin an order. `tools/spec/harness.py`, `SETUP_DECKS`.
    ('my deck at setup is "<a>", "<b>"',
     Rx(r'my deck at setup is ' + LIST),
     lambda cards: ("setting", "setup_player_deck", SplitRefs(cards))),
    ('the encounter deck at setup is "<a>", "<b>"',
     Rx(r'the encounter deck at setup is ' + LIST),
     lambda cards: ("setting", "setup_encounter_deck", SplitRefs(cards))),

    # -- zone fills --------------------------------------------------------
    ('my hand is "<a>", "<b>"',
     Rx(r'my hand is ' + LIST),
     lambda cards: ("given", GivenStep("hand", SplitRefs(cards)))),
    ('my deck is "<a>", "<b>"',
     Rx(r'my deck is ' + LIST),
     lambda cards: ("given", GivenStep("player_deck", SplitRefs(cards)))),
    ('my discard pile is "<a>", "<b>"',
     Rx(r'my discard pile is ' + LIST),
     lambda cards: ("given", GivenStep("player_discard", SplitRefs(cards)))),
    ('my set aside deck is "<a>", "<b>"',
     Rx(r'my set aside deck is ' + LIST),
     lambda cards: ("given", GivenStep("player_set_aside", SplitRefs(cards)))),
    # The per-seat form of the same step, and the reason it exists: "each player
    # puts the top card of their deck into play" is printed on 242 cards, and
    # until MARVEL-101 the second player's deck could not be given a known top
    # card, so the "each" was unassertable. `my deck is` is this step with the
    # seat left at player 1.
    ("player <n>'s deck is \"<a>\", \"<b>\"",
     Rx(r"player " + N + r"'s deck is " + LIST),
     lambda who, cards: ("given", GivenStep(
         "player_deck", SplitRefs(cards), player=int(who) - 1))),
    ('the encounter deck is "<a>", "<b>"',
     Rx(r'the encounter deck is ' + LIST),
     lambda cards: ("given", GivenStep("encounter_deck", SplitRefs(cards)))),
    ('the encounter discard pile is "<a>", "<b>"',
     Rx(r'the encounter discard pile is ' + LIST),
     lambda cards: ("given", GivenStep("encounter_discard", SplitRefs(cards)))),

    # -- magnitudes --------------------------------------------------------
    ('"<card>" has <n> damage',
     Rx(Q + r' has ' + N + r' damage'),
     lambda card, value: ("given", GivenStep("damage", (card,), value=int(value)))),
    ('"<card>" is healed <n>',
     Rx(Q + r' is healed (?:for |by )?' + N),
     lambda card, value: ("given", GivenStep("heal", (card,), value=int(value)))),
    ('"<card>" has <n> threat',
     Rx(Q + r' has ' + N + r' threat'),
     lambda card, value: ("given", GivenStep("threat", (card,), value=int(value)))),
    ('the main scheme has <n> threat',
     Rx(r'the main scheme has ' + N + r' threat'),
     lambda value: ("given", GivenStep("threat", ("the main scheme",), value=int(value)))),
    ('"<card>" has <n> "<name>" counters',
     Rx(Q + r' has ' + N + r' ' + Q + r' counters?'),
     lambda card, value, name: ("given", GivenStep(
         "counters", (card,), value=int(value), name=name))),
    ('"<card>" has <n> "<name>" tokens',
     Rx(Q + r' has ' + N + r' ' + Q + r' tokens?'),
     lambda card, value, name: ("given", GivenStep(
         "tokens", (card,), value=int(value), name=name))),

    # -- states ------------------------------------------------------------
    ('I am in hero form',
     Rx(r'i am in hero form'),
     lambda: ("given", GivenStep("hero_form", ("me",)))),
    ('I am in alter-ego form',
     Rx(r'i am in alter-ego form'),
     lambda: ("given", GivenStep("alter_ego_form", ("me",)))),
    ('"<card>" is stunned',
     Rx(Q + r' is stunned'),
     lambda card: ("given", GivenStep("stunned", (card,)))),
    ('"<card>" is confused',
     Rx(Q + r' is confused'),
     lambda card: ("given", GivenStep("confused", (card,)))),
    ('"<card>" is tough',
     Rx(Q + r' is tough'),
     lambda card: ("given", GivenStep("tough", (card,)))),
    ('"<card>" is exhausted',
     Rx(Q + r' is exhausted'),
     lambda card: ("given", GivenStep("exhausted", (card,)))),
    ('"<card>" is ready',
     Rx(Q + r' is ready'),
     lambda card: ("given", GivenStep("ready", (card,)))),
    ('"<card>" is discarded',
     Rx(Q + r' is discarded'),
     lambda card: ("given", GivenStep("discarded", (card,)))),
    ('"<card>" is in play',
     Rx(Q + r' is in play'),
     lambda card: ("given", GivenStep("in_play", (card,)))),
    ('"<card>" is revealed',
     Rx(Q + r' is revealed'),
     lambda card: ("given", GivenStep("revealed", (card,)))),
    ('I draw <n> cards',
     Rx(r'i draw ' + N + r' cards?'),
     lambda value: ("given", GivenStep("draw", value=int(value)))),
]

WHEN_TABLE: Table = [
    ('I play "<card>"',
     Rx(r'i play ' + Q),
     lambda card: ("when", WhenStep(option="play", card=card))),
    ('I play "<card>" targeting "<target>"',
     Rx(r'i play ' + Q + r' targeting ' + Q),
     lambda card, target: ("when", WhenStep(
         option="play", card=card, targets=(target,)))),
    ('I choose "<option>"',
     Rx(r'i choose ' + Q),
     lambda option: ("when", WhenStep(option=option))),
    ('I choose "<option>" targeting "<a>", "<b>"',
     Rx(r'i choose ' + Q + r' targeting ' + LIST),
     lambda option, targets: ("when", WhenStep(
         option=option, targets=SplitRefs(targets)))),
    ('I choose "<option>" on "<card>"',
     Rx(r'i choose ' + Q + r' on ' + Q),
     lambda option, card: ("when", WhenStep(option=option, card=card))),
    ('I choose "<option>" on "<card>" paying <n> resources',
     Rx(r'i choose ' + Q + r' on ' + Q + r' paying ' + N + r' resources'),
     lambda option, card, payment: ("when", WhenStep(
         option=option, card=card, payment=int(payment)))),
    ('I choose "<option>" on "<card>" paying <n> resources targeting "<a>", "<b>"',
     Rx(r'i choose ' + Q + r' on ' + Q + r' paying ' + N +
        r' resources targeting ' + LIST),
     lambda option, card, payment, targets: ("when", WhenStep(
         option=option, card=card, payment=int(payment),
         targets=SplitRefs(targets)))),
    ('I choose "<option>" on "<card>" targeting "<a>", "<b>"',
     Rx(r'i choose ' + Q + r' on ' + Q + r' targeting ' + LIST),
     lambda option, card, targets: ("when", WhenStep(
         option=option, card=card, targets=SplitRefs(targets)))),
    ('I attack "<target>"',
     Rx(r'i attack ' + Q),
     lambda target: ("when", WhenStep(option="attack", targets=(target,)))),
    ('I thwart "<target>"',
     Rx(r'i thwart ' + Q),
     lambda target: ("when", WhenStep(option="thwart", targets=(target,)))),
    ('I change form',
     Rx(r'i change form'),
     lambda: ("when", WhenStep(option="change form"))),
    ('I pass',
     Rx(r'i pass'),
     lambda: ("when", WhenStep(pass_priority=True))),
]

THEN_TABLE: Table = [
    # -- the two assertions a transcript buys ------------------------------
    ('I am prompted to choose one',
     Rx(r'i am prompted to choose one'),
     lambda: ("then", "prompt")),
    ('I am not prompted again',
     Rx(r'i am not prompted again'),
     lambda: ("then", NoPromptStep())),
    # A negative assertion over one card-bound option. Affordability cannot be
    # reduced to the hand: generators, discounts, targets and group payments
    # all contribute. Observe the option set the engine exposes instead.
    ('I am not offered "<option>" on "<card>"',
     Rx(r'i am not offered ' + Q + r' on ' + Q),
     lambda option, card: ("then", NotOfferedStep(option=option, card=card))),

    # The third assertion: something the engine will not let you do. A
    # restriction that filters an option's *targets* rather than removing the
    # option is invisible to the two above -- the option set is unchanged and no
    # card's state has changed -- and Guard is exactly that shape. See
    # `CannotStep`.
    ('I cannot attack "<card>"',
     Rx(r'i cannot attack ' + Q),
     lambda card: ("then", CannotStep(option="attack", card=card))),
    ('I cannot thwart "<card>"',
     Rx(r'i cannot thwart ' + Q),
     lambda card: ("then", CannotStep(option="thwart", card=card))),
    # The same claim over any option, which is what "remove 2 threat from a
    # *different* scheme" needs. `CannotStep` was already general; only these
    # two sentences were not. MARVEL-94.
    ('I cannot choose "<option>" targeting "<card>"',
     Rx(r'i cannot choose ' + Q + r' targeting ' + Q),
     lambda option, card: ("then", CannotStep(option=option, card=card,
                                              verb=False))),
    # ...and the positive form, for "look at the top 3 cards of your deck".
    ('the legal targets for "<option>" are',
     Rx(r'the legal targets for ' + Q + r' are'),
     lambda option: ("then", ("targets", option))),
    # How many of them may be taken, which is the other half of "up to N" and
    # the half nothing could say. Naming a fourth target for Ancestral
    # Knowledge's "up to 3" is refused with `Play takes 1..3 target(s)` -- the
    # engine right and the transcript with no passing spelling, so the printed
    # number was pinned from below only. An equality, and worded as one: see
    # `LimitStep` for why it is not spelled "takes at most <n> targets".
    ('the target maximum for "<option>" is <n>',
     Rx(r'the target maximum for ' + Q + r' is ' + N),
     lambda option, value: ("then", LimitStep(option=option,
                                              maximum=int(value)))),
    ('the target maximum for "<option>" on "<card>" is <n>',
     Rx(r'the target maximum for ' + Q + r' on ' + Q + r' is ' + N),
     lambda option, card, value: (
         "then", LimitStep(option=option, maximum=int(value), card=card))),
    # The floor counterpart. It reads the live effective range, not the raw
    # selector spelling: see `MinimumStep` for the clamp and `range="All"`
    # semantics that the C# binding must reproduce.
    ('the target minimum for "<option>" is <n>',
     Rx(r'the target minimum for ' + Q + r' is ' + N),
     lambda option, value: ("then", MinimumStep(option=option,
                                                minimum=int(value)))),
    ('the target minimum for "<option>" on "<card>" is <n>',
     Rx(r'the target minimum for ' + Q + r' on ' + Q + r' is ' + N),
     lambda option, card, value: (
         "then", MinimumStep(option=option, minimum=int(value), card=card))),

    # -- card state --------------------------------------------------------
    ('"<card>" has <n> health',
     Rx(Q + r' has ' + N + r' health'),
     lambda card, value: ("then", ThenStep(card, "health", int(value)))),
    ('"<card>" has <n> damage',
     Rx(Q + r' has ' + N + r' damage'),
     lambda card, value: ("then", ThenStep(card, "damage", int(value)))),
    ('"<card>" has <n> threat',
     Rx(Q + r' has ' + N + r' threat'),
     lambda card, value: ("then", ThenStep(card, "threat", int(value)))),
    ('the main scheme has <n> threat',
     Rx(r'the main scheme has ' + N + r' threat'),
     lambda value: ("then", ThenStep("the main scheme", "threat", int(value)))),
    ('"<card>" has <n> "<name>" counters',
     Rx(Q + r' has ' + N + r' ' + Q + r' counters?'),
     lambda card, value, name: ("then", ThenStep(card, f"counter:{name}", int(value)))),
    ('"<card>" has <n> "<name>" tokens',
     Rx(Q + r' has ' + N + r' ' + Q + r' tokens?'),
     lambda card, value, name: ("then", ThenStep(card, f"token:{name}", int(value)))),
    # The printed icons, by the name the card prints -- physical, mental, energy
    # or wild. `RES` was the one printed attribute in the payment path with no
    # reader, and it is the only thing telling 01043a/b/c/d apart: four ids, one
    # printed text, one script, four different icons. `coverage.Equivalents()`
    # rightly declines to credit them to each other over it, so the tool says
    # four cards of work while the vocabulary could express one.
    #
    # The count is of icons *printed*, not of costs payable: a wild icon pays a
    # physical cost and this still answers 0 physical. What an icon buys is
    # already observable the ordinary way -- play a card and see what the engine
    # took.
    ('"<card>" has <n> "<icon>" resource icons',
     Rx(Q + r' has ' + N + r' ' + Q + r' resource icons?'),
     lambda card, value, icon: ("then", ThenStep(card, f"resource:{icon}",
                                                 int(value)))),
    ('"<card>" is in the "<zone>"',
     Rx(Q + r' is in the ' + Q),
     lambda card, zone: ("then", ThenStep(card, "zone", zone))),
    ('"<card>" is [not] in play',
     Rx(Q + r' is (not )?in play'),
     lambda card, negated: ("then", ThenStep(card, "in_play", not negated))),
    ('"<card>" is [not] exhausted',
     Rx(Q + r' is (not )?exhausted'),
     lambda card, negated: ("then", ThenStep(card, "exhausted", not negated))),
    ('"<card>" is [not] ready',
     Rx(Q + r' is (not )?ready'),
     lambda card, negated: ("then", ThenStep(card, "ready", not negated))),
    ('"<card>" is [not] stunned',
     Rx(Q + r' is (not )?stunned'),
     lambda card, negated: ("then", ThenStep(card, "stunned", not negated))),
    ('"<card>" is [not] confused',
     Rx(Q + r' is (not )?confused'),
     lambda card, negated: ("then", ThenStep(card, "confused", not negated))),
    ('"<card>" is [not] tough',
     Rx(Q + r' is (not )?tough'),
     lambda card, negated: ("then", ThenStep(card, "tough", not negated))),
    ('"<card>" has <n> "<property>"',
     Rx(Q + r' has ' + N + r' ' + Q),
     lambda card, value, prop: ("then", ThenStep(card, prop, int(value)))),

    # -- me ----------------------------------------------------------------
    ('I am [not] in hero form',
     Rx(r'i am (not )?in hero form'),
     lambda negated: ("then", ThenStep("me", "hero_form", not negated))),
    ('I am [not] exhausted',
     Rx(r'i am (not )?exhausted'),
     lambda negated: ("then", ThenStep("me", "exhausted", not negated))),
    ('I have <n> damage',
     Rx(r'i have ' + N + r' damage'),
     lambda value: ("then", ThenStep("me", "damage", int(value)))),
    ('I have <n> cards in hand',
     Rx(r'i have ' + N + r' cards? in hand'),
     lambda value: ("then", ThenStep("player", "hand_size", int(value)))),
    ('I have <n> cards in my deck',
     Rx(r'i have ' + N + r' cards? in my deck'),
     lambda value: ("then", ThenStep("player", "deck_size", int(value)))),
    ('I have <n> cards in my discard pile',
     Rx(r'i have ' + N + r' cards? in my discard pile'),
     lambda value: ("then", ThenStep("player", "discard_size", int(value)))),
    ('I am [not] eliminated',
     Rx(r'i am (not )?eliminated'),
     lambda negated: ("then", ThenStep("player", "eliminated", not negated))),
    ('player <n> has <m> cards in hand',
     Rx(r'player ' + N + r' has ' + N + r' cards? in hand'),
     lambda who, value: ("then", ThenStep(f"player {who}", "hand_size", int(value)))),
    # The other half of MARVEL-101. Without it a two-player scenario could be
    # set up and nothing about the second player's deck could be pinned -- not
    # even the weak fallback that each deck went down by one.
    ('player <n> has <m> cards in their deck',
     Rx(r'player ' + N + r' has ' + N + r' cards? in their deck'),
     lambda who, value: ("then", ThenStep(f"player {who}", "deck_size", int(value)))),

    # -- the game ----------------------------------------------------------
    ('the game is [not] over',
     Rx(r'the game is (not )?over'),
     lambda negated: ("then", ThenStep("game", "game_over", not negated))),
    ('the players won',
     Rx(r'the players (?:have )?(lost|won)'),
     lambda outcome: ("then", ThenStep("game", "players_won", outcome.lower() == "won"))),
    ('it is round <n>',
     Rx(r'it is round ' + N),
     lambda value: ("then", ThenStep("game", "round", int(value)))),

    # Two grains, because the rulebook and the engine do not agree on how many
    # phases there are. `the villain phase` is the rulebook's three-phase round
    # and is what a rules scenario means; the quoted form names one of the
    # engine's eleven `Phase.State`s, which is what pins a *transition* -- "the
    # villain phase" cannot tell threat placement from enemy activation, and the
    # order of those two is exactly the sort of thing a port gets wrong.
    ('it is the <player|villain|end> phase',
     Rx(r'it is the (' + "|".join(PHASE_NAMES) + r') phase'),
     lambda group: ("then", ThenStep("game", "phase_group", group.lower()))),
    ('it is the "<phase>" phase',
     Rx(r'it is the ' + Q + r' phase'),
     lambda phase: ("then", ThenStep("game", "phase", phase))),
]

TABLES: Dict[str, Table] = {
    "given": GIVEN_TABLE,
    "when": WHEN_TABLE,
    "then": THEN_TABLE,
}


def CompileStep(keyword: str, text: str) -> Any:
    for _form, pattern, build in TABLES[keyword]:
        match = pattern.match(text.strip())
        if match:
            return build(*match.groups())
    return None


def Vocabulary() -> Dict[str, List[str]]:
    """The step forms this module implements, per clause.

    `unit_test/test_spec_validate.py` compares this against
    `specs/steps.catalogue.json`, so a form added here without being checked in
    -- or checked in without being implemented -- fails the build.
    """
    return {clause: [form for form, _pattern, _build in table]
            for clause, table in TABLES.items()}


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
    steps: List[Tuple[str, str, int, Tuple[str, ...]]] = field(default_factory=list)
    """(clause, text, line number, data table rows)."""

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
    outline_rows = 0
    cases: List[SpecCase] = []

    def Finish() -> None:
        nonlocal current
        if current is not None:
            cases.append(Build(current, feature_name, path, digest, where))
            current = None

    def EndOutline() -> None:
        """An outline with no Examples rows produces nothing and must not pass.

        Silently dropping it would be the one thing this parser promises never
        to do: a scenario that looks authored but never runs, and so never
        fails either.
        """
        nonlocal outline
        if outline is not None and outline_rows == 0:
            raise GherkinError(
                f"{where}:{outline.line}: Scenario Outline "
                f"{outline.name!r} has no Examples rows, so it would never run")
        outline = None

    def Target() -> Optional[Draft]:
        if in_background:
            return background
        return outline if outline is not None else current

    for number, raw in enumerate(text.splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue

        if line.startswith("@"):
            # Accumulate. Gherkin lets one scenario carry its tags over several
            # lines, and every file in `specs/cards/reprints/` uses that shape to
            # credit both ids of a pair -- one `@card:` per line. An assignment
            # here keeps only the last line, so the first id of each pair
            # silently lost every scenario written for it, and the card read as
            # uncovered while its own spec sat in the tree: the coverage number
            # moving in the one direction it must never move on its own.
            pending_tags += [tag.lstrip("@") for tag in line.split()]
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
                EndOutline()
                feature_name = title
                background = Draft()
            elif word == "background":
                Finish()
                EndOutline()
                background = Draft()
                in_background = True
            elif word in ("scenario outline", "scenario template"):
                Finish()
                EndOutline()
                outline = background.Copy()
                outline.name = title
                outline.line = number
                outline.tags = list(pending_tags)
                outline_rows = 0
                # An outline is only ever realised through its Examples rows.
                current = None
                pending_tags = []
            elif word == "scenario":
                Finish()
                EndOutline()
                current = background.Copy()
                current.name = title
                current.line = number
                current.tags = list(pending_tags)
                pending_tags = []
            else:  # Examples / Scenarios
                if outline is None:
                    raise GherkinError(f"{where}:{number}: Examples without a Scenario Outline")
                Finish()
                in_examples = True
                examples_header = []
            continue

        if line.startswith("|"):
            cells = [cell.strip() for cell in line.strip("|").split("|")]

            if in_examples:
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
                outline_rows += 1
                continue

            # Otherwise it is a data table under the step above it.
            draft = Target()
            if draft is None or not draft.steps:
                raise GherkinError(f"{where}:{number}: table row outside a step")
            past_clause, past_text, past_line, rows = draft.steps[-1]
            draft.steps[-1] = (past_clause, past_text, past_line, rows + tuple(cells))
            continue

        step = STEP.match(line)
        if not step:
            raise GherkinError(f"{where}:{number}: expected a Given/When/Then step, got {line!r}")

        word = step.group(1).lower()
        if word in ("given", "when", "then"):
            clause = word
        # And / But / * continue the clause above them, as Gherkin defines.

        draft = Target()
        if draft is None:
            raise GherkinError(f"{where}:{number}: step outside a Scenario: {line!r}")

        draft.steps.append((clause, step.group(2), number, ()))

    Finish()
    EndOutline()

    if not cases:
        raise GherkinError(f"{where}: no scenarios found")
    return cases


def Expand(outline: Draft, bindings: Dict[str, str], where: str, number: int) -> Draft:
    """One row of an Examples table, as a concrete scenario."""
    draft = outline.Copy()
    draft.steps = [(clause, Substitute(text, bindings, where, line), line, rows)
                   for clause, text, line, rows in outline.steps]
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
    setup_player_deck: List[str] = []
    setup_encounter_deck: List[str] = []
    given: List[GivenStep] = []
    beats: List[Any] = []

    for clause, text, number, rows in draft.steps:
        if PLACEHOLDER.search(text):
            raise GherkinError(
                f"{where}:{number}: {text!r} still has a <placeholder>; "
                f"a Scenario Outline needs an Examples table")

        # A builder that constructs its beat inline -- `LimitStep`, `WhenStep` --
        # validates in `__post_init__`, and that runs here rather than in the
        # block below. Unwrapped, its `SpecCaseError` escapes without the line
        # number, which is the one thing this parser promises every failure has.
        try:
            compiled = CompileStep(clause, text)
        except GherkinError:
            raise
        except SpecCaseError as exc:
            raise GherkinError(f"{where}:{number}: {exc}") from exc

        if compiled is None:
            raise GherkinError(
                f"{where}:{number}: no {clause.title()} step matches {text!r}. "
                f"See specs/steps.catalogue.json for the step vocabulary.")

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
                elif key == "setup_player_deck":
                    # Accumulates, like the `Given` deck steps it is the
                    # setup-time twin of: a deck is stocked by naming what goes
                    # into it, wherever the naming happens.
                    setup_player_deck += [str(x) for x in value]
                elif key == "setup_encounter_deck":
                    setup_encounter_deck += [str(x) for x in value]
            elif kind == "given":
                if beats:
                    raise GherkinError(
                        f"{where}:{number}: a Given cannot follow a When -- the "
                        f"board is built once, before the transcript starts")
                given.append(compiled[1])
            elif kind == "when":
                beats.append(compiled[1])
            else:
                payload = compiled[1]
                if payload == "prompt":
                    if not rows:
                        raise GherkinError(
                            f"{where}:{number}: 'I am prompted to choose one' needs "
                            f"a table of the options the engine should offer")
                    payload = PromptStep(options=tuple(rows))
                elif isinstance(payload, tuple) and payload and payload[0] == "targets":
                    if not rows:
                        raise GherkinError(
                            f"{where}:{number}: 'the legal targets for "
                            f"{payload[1]!r} are' needs a table of the cards the "
                            f"engine should accept")
                    payload = TargetsStep(option=payload[1], targets=tuple(rows))
                beats.append(payload)
        except GherkinError:
            raise
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
            setup_player_deck=tuple(setup_player_deck),
            setup_encounter_deck=tuple(setup_encounter_deck),
            tags=tuple(draft.tags),
            given=tuple(given),
            beats=tuple(beats),
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

    `validate specs` and `validate ./specs` must record the same source path, or
    every run would rewrite `trusted.json`.
    """
    text = path.replace(os.sep, "/")
    while text.startswith("./"):
        text = text[2:]
    return text
