"""Which games to play, decided deterministically from a seed.

The configuration space cannot be enumerated: 108 scenarios x 63 heroes x four
player counts is already 1.7 million hero *sets* before encounter sets and
challenges, and the cross product with those is astronomical. So the question is
not "which games" but "which sampling strategy", and this module is the answer
to that. See MARVEL-15.

A plan is a pure function of `(seed, sizes, inventory)`. Nothing here starts an
engine, reads a clock, or touches the network, so a plan can be printed,
diffed and regenerated without playing anything -- which is what makes
"regenerate the identical corpus" a checkable claim rather than a hope.
`generate.py` then plays it.

## Three phases, in order

1. **Scenario coverage.** `floor` passes over every scenario, each pass a fresh
   shuffle. This is the phase that stops a random sampler from playing Rhino
   four hundred times and Kang never.
2. **Hero top-up.** Heroes ride along in phase 1 and mostly even out by
   themselves, but only if there are enough games to go round: 108 games at an
   average of 2.5 heroes is 270 seats for 63 heroes, and a small run has far
   fewer. So the floor is finished off explicitly rather than assumed.
3. **Random fill.** Whatever budget is left, sampled uniformly.

The floor is honoured *before* the budget. If the two disagree the plan is
bigger than asked for and says so in `Warnings()` -- a run that quietly dropped
half its scenarios to hit a game count would produce exactly the corpus this
issue exists to avoid.

## Heroes are drawn least-used-first

Not uniformly at random. A uniform draw over 63 heroes leaves the tail badly
under-sampled at realistic corpus sizes -- the coupon-collector problem, and the
tail is where port bugs hide. Drawing the least-used heroes and breaking ties
with the planner's RNG means every hero is played once before any is played
twice, which is the property phase 2 then only has to top up.

## One case is a run of seeds, not one game

Engine start is ~0.5s against ~0.2-2s for a game, so a process that plays one
game spends most of its life importing. A case therefore names a run of
consecutive seeds for one `(scenario, heroes)` pair, and `generate.py` gives
each case one process.

The coverage phases deliberately use **one game per case** anyway: their whole
purpose is breadth, and batching there would multiply the floor by the batch
size. The random phase batches, because that is where throughput matters and
where another seed on the same pairing is as good as any other game.
"""

from __future__ import annotations

import hashlib
import json
import random
from typing import Any, Dict, List, NamedTuple, Sequence, Tuple

from tools.corpus.inventory import Inventory

# Player counts to sample. Every count is exercised: solo and four-player are
# different games, not the same game scaled -- per-player icons, the villain's
# hit points, and how much threat a scheme takes all move with it.
PLAYER_COUNTS: Tuple[int, ...] = (1, 2, 3, 4)

DEFAULT_FLOOR = 1
DEFAULT_GAMES_PER_CASE = 4


class Case(NamedTuple):
    """One subprocess: one pairing, played over `games` consecutive seeds."""

    index: int
    scenario: str
    heroes: Tuple[str, ...]
    seed: int
    games: int
    phase: str

    @property
    def id(self) -> str:
        """Stable identity, so a resumed run knows what it already played.

        Built from what decides the games rather than from `index`, so
        reordering a plan does not invalidate a half-finished corpus.
        """
        return (f"{self.scenario}|{'+'.join(self.heroes)}"
                f"|{self.seed}|{self.games}")

    @property
    def players(self) -> int:
        return len(self.heroes)

    def ToDict(self) -> Dict[str, Any]:
        return {
            "index": self.index,
            "id": self.id,
            "scenario": self.scenario,
            "heroes": list(self.heroes),
            "seed": self.seed,
            "games": self.games,
            "phase": self.phase,
        }


class Plan(NamedTuple):
    seed: int
    floor: int
    requested_games: int
    games_per_case: int
    player_counts: Tuple[int, ...]
    inventory_digest: str
    cases: List[Case]

    @property
    def games(self) -> int:
        return sum(case.games for case in self.cases)

    def Digest(self) -> str:
        """A hash of the cases. Two plans with the same digest play the same
        games in the same order under the same seeds."""
        payload = json.dumps([case.ToDict() for case in self.cases],
                             sort_keys=True, separators=(",", ":"))
        return hashlib.sha256(payload.encode("utf-8")).hexdigest()[:16]

    def Counts(self) -> Tuple[Dict[str, int], Dict[str, int], Dict[int, int]]:
        """(games per scenario, games per hero, cases per player count)."""
        scenarios: Dict[str, int] = {}
        heroes: Dict[str, int] = {}
        players: Dict[int, int] = {}
        for case in self.cases:
            scenarios[case.scenario] = scenarios.get(case.scenario, 0) + case.games
            for hero in case.heroes:
                heroes[hero] = heroes.get(hero, 0) + case.games
            players[case.players] = players.get(case.players, 0) + 1
        return scenarios, heroes, players

    def Warnings(self) -> List[str]:
        """Everything a reader should know that the game count does not say."""
        lines: List[str] = []
        if self.games > self.requested_games:
            lines.append(
                f"the plan holds {self.games} games rather than the "
                f"{self.requested_games} requested: covering every scenario and "
                f"hero {self.floor} time(s) needs that many. Lower -floor or "
                f"narrow the inventory to get a smaller corpus.")
        return lines

    def ToDict(self) -> Dict[str, Any]:
        return {
            "seed": self.seed,
            "floor": self.floor,
            "requested_games": self.requested_games,
            "games_per_case": self.games_per_case,
            "player_counts": list(self.player_counts),
            "inventory_digest": self.inventory_digest,
            "digest": self.Digest(),
            "games": self.games,
            "cases": [case.ToDict() for case in self.cases],
        }


class Builder:
    """Accumulates cases while tracking what has been covered so far."""

    def __init__(self, rng: 'random.Random', inventory: Inventory,
                 seed_base: int, player_counts: Sequence[int]) -> None:
        self.rng = rng
        self.inventory = inventory
        self.player_counts = list(player_counts)
        self.cases: List[Case] = []
        self.next_seed = seed_base
        # Hero seats handed out so far. Drives the least-used-first draw.
        self.hero_games: Dict[str, int] = {hero: 0 for hero in inventory.heroes}

    def DrawHeroes(self, count: int, games: int) -> Tuple[str, ...]:
        """The `count` least-used heroes, ties broken by the planner's RNG.

        Shuffling *before* the sort is what breaks ties: `sorted` is stable, so
        it preserves the shuffled order inside each equal-count group. Sorting a
        fixed list would hand out heroes in alphabetical order forever and make
        `adam_warlock` appear in every game.

        Counted in games rather than in draws, because a hero drawn once is
        seated for every game in the case.
        """
        count = min(count, len(self.inventory.heroes))
        pool = list(self.inventory.heroes)
        self.rng.shuffle(pool)
        pool.sort(key=lambda hero: self.hero_games[hero])

        chosen = tuple(pool[:count])
        for hero in chosen:
            self.hero_games[hero] += games
        return chosen

    def Add(self, scenario: str, players: int, games: int, phase: str) -> Case:
        case = Case(
            index=len(self.cases),
            scenario=scenario,
            heroes=self.DrawHeroes(players, games),
            seed=self.next_seed,
            games=games,
            phase=phase,
        )
        self.next_seed += games
        self.cases.append(case)
        return case

    def Players(self, index: int) -> int:
        """Cycle the player counts rather than sampling them.

        A sample of four values over a hundred draws is lumpy, and player count
        is the axis most likely to be *systematically* under-covered by bad
        luck. Cycling makes the distribution exact.
        """
        return self.player_counts[index % len(self.player_counts)]


def Build(inventory: Inventory, *, seed: int, games: int,
          floor: int=DEFAULT_FLOOR,
          games_per_case: int=DEFAULT_GAMES_PER_CASE,
          player_counts: Sequence[int]=PLAYER_COUNTS,
          seed_base: int|None=None) -> Plan:
    """Decide which games to play. Pure: same arguments, same plan."""
    if not inventory.scenarios or not inventory.heroes:
        raise ValueError("cannot plan against an empty inventory")
    if games < 0:
        raise ValueError(f"games must not be negative, got {games}")

    counts = [count for count in player_counts if count >= 1]
    if not counts:
        raise ValueError("no usable player counts")

    rng = random.Random(seed)
    base = seed if seed_base == None else seed_base
    builder = Builder(rng, inventory, seed_base=base, player_counts=counts)

    # --- phase 1: every scenario, `floor` times ---------------------------
    for _ in range(max(0, floor)):
        sweep = list(inventory.scenario_names)
        rng.shuffle(sweep)
        for scenario in sweep:
            builder.Add(scenario, builder.Players(len(builder.cases)), 1,
                        "scenario-coverage")

    # --- phase 2: finish the hero floor -----------------------------------
    # Only bites on a run too small for phase 1 to have seated everyone.
    while builder.hero_games and min(builder.hero_games.values()) < max(0, floor):
        scenario = rng.choice(inventory.scenario_names)
        builder.Add(scenario, builder.Players(len(builder.cases)), 1,
                    "hero-coverage")

    # --- phase 3: random fill ---------------------------------------------
    batch = max(1, games_per_case)
    while builder.next_seed - base < games:
        remaining = games - (builder.next_seed - base)
        scenario = rng.choice(inventory.scenario_names)
        builder.Add(scenario, builder.Players(len(builder.cases)),
                    min(batch, remaining), "random")

    return Plan(
        seed=seed,
        floor=floor,
        requested_games=games,
        games_per_case=batch,
        player_counts=tuple(counts),
        inventory_digest=inventory.Digest(),
        cases=builder.cases,
    )


def Summarize(plan: Plan) -> List[str]:
    """What the plan covers, for a person about to spend hours on it."""
    scenarios, heroes, players = plan.Counts()
    lines = [
        f"{len(plan.cases)} case(s), {plan.games} game(s), "
        f"plan digest {plan.Digest()}",
        f"scenarios covered {len(scenarios)}  "
        f"(min {min(scenarios.values(), default=0)} games, "
        f"max {max(scenarios.values(), default=0)})",
        f"heroes covered    {len(heroes)}  "
        f"(min {min(heroes.values(), default=0)} games, "
        f"max {max(heroes.values(), default=0)})",
        "players           " + ", ".join(
            f"{count}p x{players.get(count, 0)}" for count in plan.player_counts),
    ]
    lines += plan.Warnings()
    return lines
