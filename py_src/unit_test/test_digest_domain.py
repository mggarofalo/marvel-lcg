"""Digest strings stay inside the range where both engines' JSON writers agree.

The digest is compared byte for byte between the Python engine and the C# port,
so the *spelling* of the JSON is the contract, not a formatting preference. That
raises a question `docs/state-digest-v2.md` did not originally answer: can the
two languages' native JSON writers be made to agree?

Not in general, and the reason is small and unfixable. Python writes an escape
as `\\u001f`; .NET writes `\\u001F`. Hex case is not configurable on either side.
So the moment a character needs escaping *at all*, the two disagree, and no
combination of settings repairs it.

They agree everywhere else. `json.dumps` and .NET's
`JavaScriptEncoder.UnsafeRelaxedJsonEscaping` produce identical bytes for every
string that needs no escape -- which is every string a digest actually contains,
because a digest holds identifiers: card ids, zone names and field names. Never
a card *name*, which is where the accents and curly apostrophes live.

That is a constraint, not a happy accident, and this is where it is enforced.
The C# side checks the recorded fixture; only here is the whole domain visible.

Measured when this landed: 3,999 card ids, 96 zone names and 257 field names --
4,352 strings, all printable ASCII, all byte-identical between the two writers.
The narrowest margin is the apostrophe: three traits carry one, and it is the
only character in the real domain where .NET's *default* encoder would have
diverged. Hence `UnsafeRelaxedJsonEscaping` on that side, spelled out rather
than defaulted.

    python -m unittest unit_test.test_digest_domain
"""

from __future__ import annotations

import json
import re
import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.deck.deck_type import DeckType
from game.world import digest

CARDS_JSON = "data/cards.json"

# Printable ASCII. Below is a control character, above is where `ensure_ascii`
# starts escaping and .NET stops matching.
SAFE = re.compile(r"^[\x20-\x7e]*$")


def _Cards() -> list[dict]:
    with open(CARDS_JSON, encoding="utf-8") as handle:
        packs = json.load(handle)
    return [
        record
        for records in packs.values() if isinstance(records, list)
        for record in records if isinstance(record, dict)
    ]


def _Domain() -> dict[str, list[str]]:
    """Every string a v2 digest can contain, by where it comes from."""
    zones = []
    for member in DeckType:
        zones.append(member.name)
        zones.append(member.name + digest.SUFFIX_REMOVED)
        zones.append(member.name + digest.SUFFIX_ABSENT)

    cards = _Cards()
    return {
        "zone": zones,
        "card_id": sorted({record.get("card_id", "") for record in cards}),
        # `CardFace.GetStateFields` merges three namespaces; the fixed keys are
        # identifiers by construction, and the open-ended one is a `t_` key per
        # trait. That is the half that follows the card data, so that is the
        # half worth sweeping.
        "field": sorted({
            "t_" + trait
            for record in cards
            for trait in (record.get("traits") or [])
            if isinstance(trait, str)
        }),
    }


class TestTheDigestDomainIsPrintableAscii(unittest.TestCase):

    def setUp(self):
        self.domain = _Domain()

    def testEveryStringIsPrintableAscii(self):
        for source, values in self.domain.items():
            self.assertTrue(values, f"no {source} values found")
            for value in values:
                with self.subTest(source=source, value=value):
                    self.assertRegex(value, SAFE)

    def testEscapingIsNeverActuallyInvoked(self):
        """The property the C# side depends on, stated directly.

        If no string needs an escape, `ensure_ascii` has nothing to do, and a
        .NET writer that escapes only what JSON requires produces the same
        bytes. This is the whole reason the port does not need a hand-written
        encoder.
        """
        for source, values in self.domain.items():
            for value in values:
                with self.subTest(source=source, value=value):
                    self.assertEqual(
                        json.dumps(value, ensure_ascii=True),
                        json.dumps(value, ensure_ascii=False))

    def testTheApostropheIsTheNarrowMargin(self):
        """Three traits carry one, and it is what rules out .NET's default.

        Recorded so that a future reader who wonders why the encoder is named
        explicitly does not have to re-derive it -- and so that these three stop
        being invisible if the card data changes.
        """
        carriers = sorted(v for v in self.domain["field"] if "'" in v)
        self.assertEqual(
            carriers,
            ["t_'POOL", "t_BATROC'S BRIGADE", "t_CROSSFIRE'S CREW"])
        for value in carriers:
            self.assertEqual(json.dumps(value), '"' + value + '"')

    def testTheGuardRejectsWhatWouldDiverge(self):
        """A negative control: the characters the two writers disagree on."""
        for label, value in [
                ("curly apostrophe", "Spider’s"),
                ("e-acute", "é"),
                ("non-breaking space", " "),
                ("line separator", " "),
                ("byte-order mark", "﻿"),
                ("hex-case escape", "a\x1fb"),
                ("DEL", "a\x7fb"),
                ("outside the BMP", "\U0001f600")]:
            with self.subTest(label=label):
                self.assertNotRegex(value, SAFE)


if __name__ == "__main__":
    unittest.main()
