"""Index the card scripts under `cards/pack/` and describe what each one does.

Two jobs:

1. **Resolve card -> script.** The engine computes this at runtime inside
   `CardsDB.FindAbilities` (`cards/database.py:113`) from the card's pack and
   cleaned set name, following one hop of `ability_link` or `full_link` first.
   Mirrored here so the dataset can state, per card, which file implements it.

2. **Describe the script.** Enough shape to plan the port and to vary spec depth
   by card complexity, without pretending to understand the ability. Three
   signals, each a fact about the syntax tree rather than a judgement:
   whether the script contains an imperative handler, which player-choice APIs
   it calls, and which `AbilityFactory` triggers it registers.
"""

from __future__ import annotations

import ast
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Set

PACK_ROOT = Path("cards/pack")
PLAYER_ASK_SOURCE = Path("game/player/model/player_ask.py")

# `PlayerAsk` is a mixin of prompts; `GetPlayer` is its accessor, not a prompt.
_NOT_A_PROMPT = frozenset({"GetPlayer"})

# Choice entry points that live on `PlayerAction` rather than `PlayerAsk`
# (`game/player/action/player_action.py`). Card scripts reach for these
# directly and every one of them ends in `Controller.ChoiceOne`.
_ACTION_CHOICE_API = frozenset({
    "ChooseAbilities", "MayChooseOneAbility", "AskSpendResources",
})

# Deliberately *not* player choice, despite the names: `ChooseRandom`,
# `RandomChoice`, `ChooseSetAsideVillainAtRandom` and friends draw from the
# seeded RNG. They suspend nothing.


def PlayerChoiceApi(root: Path) -> List[str]:
    """The engine methods that suspend a card's resolution for a player answer.

    Derived from the source rather than hardcoded, so that a prompt added to
    `PlayerAsk` is counted the next time the dataset is generated instead of
    silently going missing. The resolved list is written into `summary.json`,
    which is what keeps the stratification figures honest -- a count means
    nothing without the rule that produced it.
    """
    path = root / PLAYER_ASK_SOURCE
    if not path.exists():
        raise FileNotFoundError(
            f"{path} is missing. `PlayerAsk` moved; the player-choice rule in "
            f"{__name__} has to move with it."
        )
    tree = ast.parse(path.read_text(encoding="utf-8"))
    names: Set[str] = set(_ACTION_CHOICE_API)
    for node in ast.walk(tree):
        if not isinstance(node, ast.ClassDef) or node.name != "PlayerAsk":
            continue
        for member in node.body:
            if isinstance(member, ast.FunctionDef) and not member.name.startswith("_"):
                if member.name not in _NOT_A_PROMPT:
                    names.add(member.name)
    return sorted(names)


@dataclass
class ScriptFacts:
    path: str
    lines: int
    # False when the script only builds `AbilityFactory` declarations -- no
    # nested function, so nothing imperative to port by hand.
    has_imperative_handler: bool
    player_choice_calls: List[str] = field(default_factory=list)
    ability_factories: List[str] = field(default_factory=list)


def _CalledNames(tree: ast.AST) -> Set[str]:
    names: Set[str] = set()
    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        func = node.func
        if isinstance(func, ast.Attribute):
            names.add(func.attr)
        elif isinstance(func, ast.Name):
            names.add(func.id)
    return names


def _AbilityFactories(tree: ast.AST) -> Set[str]:
    """`AbilityFactory.AfterPlayerPlayedCard(...)` -> `AfterPlayerPlayedCard`."""
    names: Set[str] = set()
    for node in ast.walk(tree):
        if not isinstance(node, ast.Call):
            continue
        func = node.func
        if (isinstance(func, ast.Attribute)
                and isinstance(func.value, ast.Name)
                and func.value.id == "AbilityFactory"):
            names.add(func.attr)
    return names


_FUNCTION_NODES = (ast.FunctionDef, ast.AsyncFunctionDef)


def _HasNestedFunction(tree: ast.AST) -> bool:
    """Is any function defined inside another function?

    Parentage, not name matching. A nested handler that happens to share a name
    with a top-level function is still nested, and a name-based test would call
    such a script purely declarative -- quietly moving a card into the stratum
    that gets the least spec attention.
    """
    for node in ast.walk(tree):
        if not isinstance(node, _FUNCTION_NODES):
            continue
        for descendant in ast.walk(node):
            if descendant is not node and isinstance(descendant, _FUNCTION_NODES):
                return True
    return False


def Analyse(path: Path, relative: str, choice_api: Set[str]) -> ScriptFacts:
    source = path.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=relative)

    called = _CalledNames(tree)
    return ScriptFacts(
        path=relative,
        lines=len(source.splitlines()),
        # A card script's module level is `GetAbilities`; anything defined
        # inside it is a handler the engine calls back into.
        has_imperative_handler=_HasNestedFunction(tree),
        player_choice_calls=sorted(called & choice_api),
        ability_factories=sorted(_AbilityFactories(tree)),
    )


@dataclass
class ScriptIndex:
    facts: Dict[str, ScriptFacts] = field(default_factory=dict)
    choice_api: List[str] = field(default_factory=list)
    # Scripts no card resolves to. Campaign and setup modules live here too --
    # they are engine code that happens to sit under `cards/pack/`.
    unclaimed: List[str] = field(default_factory=list)

    def Resolve(self, card_id: str, pack: str, clean_set: str) -> ScriptFacts | None:
        """Mirror of the module search in `FindAbilities` (`cards/database.py:193`).

        The set-name subdirectory is tried before the pack root, and a
        `*_nemesis` set falls back to the hero's own directory.
        """
        candidates: List[str] = []
        if clean_set:
            candidates.append(f"{PACK_ROOT.as_posix()}/{pack}/{clean_set}/{card_id}.py")
        candidates.append(f"{PACK_ROOT.as_posix()}/{pack}/{card_id}.py")
        if clean_set.endswith("_nemesis"):
            hero_set = clean_set[:-len("_nemesis")]
            candidates.append(f"{PACK_ROOT.as_posix()}/{pack}/{hero_set}/{card_id}.py")
        for candidate in candidates:
            found = self.facts.get(candidate)
            if found is not None:
                return found
        return None


def Index(root: Path = Path(".")) -> ScriptIndex:
    index = ScriptIndex(choice_api=PlayerChoiceApi(root))
    choice_api = set(index.choice_api)
    for path in sorted((root / PACK_ROOT).rglob("*.py")):
        if path.name == "__init__.py":
            continue
        relative = path.relative_to(root).as_posix()
        index.facts[relative] = Analyse(path, relative, choice_api)
    return index
