"""The engine defaults to the rules version it is measured against.

`game/world/world_rule.py` carries versioned rule switches. They were added as
opt-in flags and left off, so the engine implemented the pre-v1.6 reading
everywhere -- spec suite, corpus generator and self-play alike -- while the
vendored authority in `datasets/rules-reference/` is Rules Reference v1.8
(MARVEL-170).

## Why this is a test rather than a comment

The spec suite cannot see the difference. Flipping all five flags left its
verdict *exactly* unchanged: 456 scenarios, 444 PASS, 7 FAIL-spec-wrong,
5 FAIL-engine-suspected, before and after.

That is not evidence the flip is safe. It is evidence of the coverage hole
`tools.rules.coverage` reports, measured a different way -- none of these five
rules is cited by any scenario, so nothing asserts them. Generating the same
plan under each setting settles what the suite could not: **122 of 180 scenes
differed, 67.8%.** A change that alters two thirds of generated games and
nothing in the spec suite is exactly the kind that reverts unnoticed.

So the defaults are pinned here, each against the clause that decides it.
"""

from __future__ import annotations

import json
import os
import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

RULES_INDEX = os.path.join("..", "datasets", "rules-reference", "index.json")

# flag -> the RR v1.8 citations that make True the correct default.
CITED = {
    "v16_reveal": ("rr:deal-deal-an-encounter-card", "rr:surge.2"),
    "v16_teamwork": ("rr:teamwork.1",),
    "v16_player_elimination": ("rr:player-elimination.3",),
    "v16_referential_ability": ("rr:referential-ability",),
    "v16_confuse_stun": ("rr:attack-player-ability-type.1.1", "rr:thwart.1.1"),
}


class TestRulesVersionDefaults(unittest.TestCase):

    def setUp(self):
        from game.world.world_rule import WorldRule
        self.rule = WorldRule()

    def test_the_v16_behaviours_are_on_by_default(self):
        for flag in CITED:
            with self.subTest(flag):
                self.assertTrue(bool(getattr(self.rule, flag)),
                                f"{flag} must default on: it is what RR v1.8 "
                                f"says ({', '.join(CITED[flag])})")

    def test_the_bulk_switches_stay_off(self):
        """`SetRule` applies these *after* the individual flags.

        Defaulting `v16_all` on would override an explicit `no_v16_teamwork`
        in a scene's rules list, which is the opposite of what a bulk
        convenience switch should do.
        """
        self.assertFalse(bool(self.rule.v16_all))
        self.assertFalse(bool(self.rule.v15_all))

    def test_the_old_reading_is_still_selectable(self):
        """Turning a behaviour back off must remain possible per scene."""
        self.rule.SetRule(["no_v16_teamwork"], is_puzzle=False, seed=1)
        self.assertFalse(bool(self.rule.v16_teamwork))
        self.assertTrue(bool(self.rule.v16_confuse_stun))

    def test_v15_all_still_selects_the_older_set(self):
        self.rule.SetRule(["v15_all"], is_puzzle=False, seed=1)
        for flag in CITED:
            with self.subTest(flag):
                self.assertFalse(bool(getattr(self.rule, flag)))

    def test_every_cited_rule_exists_in_the_index(self):
        """A citation that names nothing is not a justification."""
        if not os.path.exists(RULES_INDEX):
            self.skipTest("no rules index present")
        with open(RULES_INDEX, encoding="utf-8") as handle:
            known = {record["id"] for record in json.load(handle)["entries"]}
        for flag, citations in CITED.items():
            for citation in citations:
                with self.subTest(flag=flag, citation=citation):
                    self.assertIn(citation, known)


if __name__ == "__main__":
    unittest.main()
