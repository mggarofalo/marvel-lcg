"""What there is to sample from, and a digest that says whether it moved.

Read off the data folders rather than out of a booted engine, because planning a
corpus must not cost an engine start -- and because a plan has to be
reproducible from the repository alone, months later, by something that is not
this engine.

The names here are exactly the strings `-bot_scenario` and `-bot_heroes` take:
the file stems. `BotRunner` passes them straight into `NewGameDescriptor`, the
same way the `/new` route does.

## Standard and expert are two scenarios, not one with a flag

`data/scenarios/` holds 108 files, of which 52 are `<name>_expert` and 56 are
not. Every expert file has a standard counterpart; four standard scenarios have
no expert form (`captain_america`, `captain_marvel`, `iron_man`,
`spider_woman`). They are separate encounter decks with different cards, so the
sampler treats them as separate scenarios and covering "every scenario" covers
both forms. Pairing them is still available -- `Scenario.standard` -- for a
caller that wants to reason about difficulty rather than about coverage.
"""

from __future__ import annotations

import hashlib
import json
import os
from typing import Any, Dict, List, NamedTuple, Sequence

# Relative to `py_src/`, like everything else the engine reads. See AGENTS.md,
# "Run everything from py_src/".
SCENARIO_FOLDER = os.path.join("data", "scenarios")
HERO_FOLDER = os.path.join("deck", "starter")

EXPERT_SUFFIX = "_expert"


class Scenario(NamedTuple):
    name: str

    @property
    def is_expert(self) -> bool:
        return self.name.endswith(EXPERT_SUFFIX)

    @property
    def standard(self) -> str:
        """The standard form of this scenario, which may be itself."""
        if self.is_expert:
            return self.name[:-len(EXPERT_SUFFIX)]
        return self.name


class Inventory(NamedTuple):
    scenarios: List[Scenario]
    heroes: List[str]

    @property
    def scenario_names(self) -> List[str]:
        return [scenario.name for scenario in self.scenarios]

    def Digest(self) -> str:
        """A short hash of what was on disk when this was read.

        A plan is only reproducible against the inventory it was drawn from: add
        a scenario file and the same seed picks different games. Recording this
        beside the plan turns "the corpus will not regenerate" from a mystery
        into one line of output.
        """
        payload = json.dumps(
            {"scenarios": self.scenario_names, "heroes": self.heroes},
            sort_keys=True, separators=(",", ":"))
        return hashlib.sha256(payload.encode("utf-8")).hexdigest()[:16]

    def Describe(self) -> str:
        expert = sum(1 for scenario in self.scenarios if scenario.is_expert)
        return (f"{len(self.scenarios)} scenarios ({expert} expert), "
                f"{len(self.heroes)} heroes, digest {self.Digest()}")

    def ToDict(self) -> Dict[str, Any]:
        return {
            "digest": self.Digest(),
            "scenarios": len(self.scenarios),
            "heroes": len(self.heroes),
        }


def Stems(folder: str) -> List[str]:
    """Sorted `.json` file stems in `folder`. Sorted, because a plan is a
    function of this list and `os.listdir` order is a filesystem detail."""
    if not os.path.isdir(folder):
        return []
    return sorted(name[:-len(".json")] for name in os.listdir(folder)
                  if name.endswith(".json")
                  and os.path.isfile(os.path.join(folder, name)))


def Read(scenario_folder: str=SCENARIO_FOLDER,
         hero_folder: str=HERO_FOLDER) -> Inventory:
    return Inventory(
        scenarios=[Scenario(name) for name in Stems(scenario_folder)],
        heroes=Stems(hero_folder),
    )


def Check(inventory: Inventory) -> List[str]:
    """Reasons this inventory cannot be sampled from. Empty means it can."""
    problems: List[str] = []
    if not inventory.scenarios:
        problems.append(
            f"no scenarios found in {SCENARIO_FOLDER}/ -- run from py_src/")
    if not inventory.heroes:
        problems.append(
            f"no hero decks found in {HERO_FOLDER}/ -- run from py_src/")
    return problems


def Subset(inventory: Inventory, scenarios: Sequence[str]=(),
           heroes: Sequence[str]=()) -> Inventory:
    """Narrow an inventory to named members, for a targeted run.

    An unknown name is dropped rather than raising: the caller gets an inventory
    that `Check` will reject if nothing survived, which reports every problem at
    once instead of the first one.
    """
    keep_scenarios = set(scenarios)
    keep_heroes = set(heroes)
    return Inventory(
        scenarios=[s for s in inventory.scenarios
                   if not keep_scenarios or s.name in keep_scenarios],
        heroes=[h for h in inventory.heroes
                if not keep_heroes or h in keep_heroes],
    )
