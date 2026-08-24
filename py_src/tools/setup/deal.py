"""The order cards are created in, as a function of the setup dataset.

`datasets/setup/setup.json` says *what* a board is made of. This says *in what
order the engine makes it*, which is a separate contract and a stricter one: a
card's `object_id` is its position in this sequence, and `object_id` is on the
wire in every state digest (`docs/state-digest-v2.md`, checklist item 1 --
"everything else depends on this").

Read out of the engine rather than invented:

    game/event/manager.py:RegisterPlayRule      the rules card, then challenges
    game/player/element/player_setup.py:SelectIdentity
                                               identity, obligations, nemesis,
                                               hero deck then player deck
    game/world/world.py:SelectScenario          main schemes, then the villain
    game/world/world.py:Initialize              scenario encounters, then each
                                               encounter set in order

Two things this does not do, because neither affects allocation and both belong
to the step after it: it does not shuffle, and it does not say where a card ends
up. An obligation is *created* into its player's nemesis pile and *moved* onto
the encounter deck before the shuffle; both facts are true and only the first
one is an id.

`Creation.spec` is a comma-separated face list exactly as the engine's card data
spells it, and **the first face is the one that starts face up** -- with one
published exception, recorded in `docs/setup-dataset.md`: a main scheme is
created `a,b` and flipped to its `b` side by `PutIntoPlay`.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, List, Sequence

# `player` on a creation that belongs to the scenario rather than a seat.
SCENARIO = -1


@dataclass(frozen=True)
class Creation:
    """One card, at the moment the engine allocates its id."""

    spec: str
    """Comma-separated face ids, e.g. `01001b,01001a`. One card, not two."""

    source: str
    """Which setup step asked for it. See `SOURCES`."""

    player: int
    """The seat it was created for, or `SCENARIO`."""

    @property
    def faces(self) -> List[str]:
        return self.spec.split(",")


SOURCES = (
    "rules",            # the `rule_a,rule_b` pseudo-card every world has
    "challenge",        # a campaign-level challenge card, if the campaign has any
    "identity",         # the hero, b-face first
    "obligation",       # set aside into the player's nemesis pile
    "nemesis",          # the rest of the nemesis set
    "hero_deck",        # the identity's signature cards
    "player_deck",      # the aspect cards
    "main_scheme",      # one per entry in `campaign.schemes`
    "villain",          # every stage, in printed order
    "encounter",        # `campaign.encounters`
    "encounter_set",    # each named set, `encounter_sets` then `modular_sets`
)


def MoveBToFront(spec: str) -> str:
    """The engine's `move_b_to_front` (`player_setup.py:216`).

    An identity is printed `a,b` -- hero side, alter-ego side -- and the game
    begins in alter-ego form, so the engine reorders the spec rather than
    flipping the card afterwards. That reordering is why the digest's `card` for
    a hero at step 0 is the `b` id.
    """
    parts = spec.split(",")
    return ",".join([p for p in parts if p.endswith("b")]
                    + [p for p in parts if not p.endswith("b")])


def DealOrder(setup: Dict[str, Any], campaign_name: str,
              hero_names: Sequence[str]) -> List[Creation]:
    """Every card the engine creates during setup, in allocation order.

    `setup` is a parsed `datasets/setup/setup.json`. Raises `KeyError` on a name
    the dataset does not hold, which is the right failure: a port that cannot
    resolve a name must not deal a board that is quietly one card short.
    """
    campaign = setup["campaigns"][campaign_name]
    creations: List[Creation] = [Creation("rule_a,rule_b", "rules", SCENARIO)]

    for challenge in campaign["challenges"]:
        creations.append(Creation(challenge, "challenge", SCENARIO))

    for seat, hero_name in enumerate(hero_names):
        hero = setup["heroes"][hero_name]
        for spec in hero["hero"]:
            creations.append(Creation(MoveBToFront(spec), "identity", seat))
        for spec in hero["obligations"]:
            creations.append(Creation(spec, "obligation", seat))
        for spec in hero["nemesis_set"]:
            creations.append(Creation(spec, "nemesis", seat))
        # One `GenerateCards` call over the concatenation, so the two lists are
        # a single run of ids rather than two.
        for spec in hero["hero_deck"]:
            creations.append(Creation(spec, "hero_deck", seat))
        for spec in hero["player_deck"]:
            creations.append(Creation(spec, "player_deck", seat))

    for spec in campaign["schemes"]:
        creations.append(Creation(spec, "main_scheme", SCENARIO))
    for spec in campaign["villain"]:
        creations.append(Creation(spec, "villain", SCENARIO))

    for spec in campaign["encounters"]:
        creations.append(Creation(spec, "encounter", SCENARIO))
    for set_name in EncounterSetNames(campaign):
        for spec in setup["encounter_sets"][set_name]["encounters"]:
            creations.append(Creation(spec, "encounter_set", SCENARIO))

    return creations


def EncounterSetNames(campaign: Dict[str, Any]) -> List[str]:
    """The named sets that go into the encounter deck, in order.

    `SceneLoader.NewFromJson` appends `modular_sets` to `encounter_sets` when
    the caller names no sets of its own, and `WhenGameBeginSetup` copies the
    result. Keeping the two fields separate in the dataset and joining them here
    means a port can also express the other case -- a scenario played with
    chosen modulars -- without the dataset having pre-decided it.
    """
    return list(campaign["encounter_sets"]) + list(campaign["modular_sets"])
