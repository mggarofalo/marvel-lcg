"""Tests for the card-text extraction tooling (`tools/cards/`, MARVEL-19).

Two layers. Most tests build a small synthetic tree so a rule can be stated and
checked in isolation -- the loader quirks these mirror are exactly the ones that
are easy to get subtly wrong. A handful run against the real repository, because
the thing that actually matters is that the checked-in dataset is reproducible
and internally consistent.

No engine bootstrap: `tools/cards/` is stdlib-only, so this runs anywhere.

    python -m unittest unit_test.test_card_dataset
"""

import dataclasses
import json
import tempfile
import unittest
from pathlib import Path

from tools.cards import (
    anomalies, deckbuilding, engine, extract, marvelsdb, scripts)
from tools.cards.text import IsCorrupt, ToPlainText

REPO = Path(".")
SNAPSHOT = REPO / extract.SNAPSHOT_DIR


def WriteJson(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload), encoding="utf-8")


def MakeEngineTree(root: Path, packs: dict, sets_info: dict | None = None) -> None:
    """A `data/` directory shaped like the engine's."""
    WriteJson(root / "data/cards.json", packs)
    WriteJson(root / "data/sets_info.json", sets_info or {})


def MakeCard(card_id: str, **overrides) -> dict:
    card = {
        "card_id": card_id,
        "type": "Ally",
        "name": "Nameless",
        "subtitle": "",
        "desc": {},
        "traits": [],
        "set_name": "",
        "text": "",
    }
    card.update(overrides)
    return card


# `PlayerAsk` is parsed out of the engine source, so a synthetic tree needs a
# stand-in for it before the script index will build.
FAKE_PLAYER_ASK = '''
class PlayerAsk:
    def GetPlayer(self): ...
    def AskChooseFace(self): ...
    def DiscardHandCards(self): ...
    def _Private(self): ...
'''


def MakeScriptTree(root: Path, files: dict) -> None:
    ask = root / scripts.PLAYER_ASK_SOURCE
    ask.parent.mkdir(parents=True, exist_ok=True)
    ask.write_text(FAKE_PLAYER_ASK, encoding="utf-8")
    for relative, body in files.items():
        path = root / scripts.PACK_ROOT / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(body, encoding="utf-8")


class TestPlainText(unittest.TestCase):

    def test_strips_formatting_but_keeps_the_words(self):
        self.assertEqual(
            ToPlainText("<b>Forced Response</b>: draw <i>1</i> card."),
            "Forced Response: draw 1 card.",
        )

    def test_keeps_resource_icons_and_card_references(self):
        # These are printed symbols, not markup. A spec author needs them.
        self.assertEqual(
            ToPlainText("Generate a [mental] resource for [[Black Panther]]."),
            "Generate a [mental] resource for [[Black Panther]].",
        )

    def test_face_separator_becomes_a_line_break(self):
        self.assertEqual(ToPlainText("Front side.<hr />Back side."),
                         "Front side.\nBack side.")

    def test_entities_are_unescaped(self):
        # data/cards.json writes the same arrow both ways; without unescaping,
        # two identical texts would compare as different.
        self.assertEqual(ToPlainText("Exhaust &#8594; draw."),
                         ToPlainText("Exhaust → draw."))

    def test_detects_the_replacement_character(self):
        self.assertTrue(IsCorrupt("Morphogenetics � Response"))
        self.assertFalse(IsCorrupt("Morphogenetics — Response"))


class TestEngineLoader(unittest.TestCase):
    """The `Paper.Load` / `CardsDB.Initialize` rules mirrored in `engine.py`."""

    def Load(self, packs, sets_info=None):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            MakeEngineTree(root, packs, sets_info)
            return engine.Load(root)

    def test_star_prefix_marks_unique_and_is_not_part_of_the_name(self):
        data = self.Load({"core": [MakeCard("01002", name="* Black Cat")]})
        card = data.cards["01002"]
        self.assertTrue(card.unique)
        self.assertEqual(card.name, "Black Cat")

    def test_plain_name_is_not_unique(self):
        data = self.Load({"core": [MakeCard("01003", name="Aunt May")]})
        self.assertFalse(data.cards["01003"].unique)
        self.assertEqual(data.cards["01003"].name, "Aunt May")

    def test_challenge_cards_carry_only_text(self):
        # `Paper.Load` takes a different branch for Challenge: no subtitle,
        # attributes, traits or set, and `text` is required rather than optional.
        data = self.Load({"challenges": [
            {"card_id": "2401", "type": "Challenge", "name": "Lava",
             "text": "Win without touching the floor."},
        ]})
        card = data.cards["2401"]
        self.assertEqual(card.text, "Win without touching the floor.")
        self.assertEqual(card.subtitle, "")
        self.assertEqual(card.attributes, {})
        self.assertEqual(card.traits, [])
        self.assertEqual(card.set_name, "")

    def test_missing_text_field_is_allowed_off_the_challenge_branch(self):
        entry = MakeCard("01094", name="Ultron")
        del entry["text"]
        data = self.Load({"core": [entry]})
        self.assertEqual(data.cards["01094"].text, "")

    def test_full_link_copies_the_source_under_a_new_id(self):
        data = self.Load({
            "gob": [
                MakeCard("02019", name="* Goblin Glider", text="Attach to the enemy.",
                         set_name="Mutagen Formula", traits=["ATTACK"]),
                {"card_id": "02033", "full_link": "02019"},
            ],
        })
        reprint = data.cards["02033"]
        self.assertEqual(reprint.text, "Attach to the enemy.")
        self.assertEqual(reprint.name, "Goblin Glider")
        self.assertEqual(reprint.traits, ["ATTACK"])
        # A reprint inherits the source's pack and set -- that is what sends
        # `FindAbilities` to the source's script rather than looking for its own.
        self.assertEqual(reprint.set_name, "Mutagen Formula")
        self.assertEqual(data.full_link["02033"], "02019")
        self.assertEqual(reprint.link_kind, "full")

    def test_reprint_does_not_alias_the_source_collections(self):
        data = self.Load({
            "gob": [
                MakeCard("02019", traits=["ATTACK"], desc={"Cost": "2"}),
                {"card_id": "02033", "full_link": "02019"},
            ],
        })
        data.cards["02033"].traits.append("MUTATED")
        data.cards["02033"].attributes["Cost"] = "9"
        self.assertEqual(data.cards["02019"].traits, ["ATTACK"])
        self.assertEqual(data.cards["02019"].attributes, {"Cost": "2"})

    def test_duplicate_card_id_keeps_the_first_and_is_reported(self):
        data = self.Load({
            "core": [MakeCard("01002", name="First")],
            "gob": [MakeCard("01002", name="Second")],
        })
        self.assertEqual(data.cards["01002"].name, "First")
        self.assertEqual(data.duplicate_ids, [("01002", "gob")])

    def test_dangling_links_are_reported_not_raised(self):
        data = self.Load({
            "core": [
                MakeCard("01043b", ability_link="nope"),
                {"card_id": "02033", "full_link": "missing"},
            ],
        })
        self.assertEqual(
            sorted(data.dangling_links),
            [("01043b", "ability", "nope"), ("02033", "full", "missing")],
        )
        # A link that goes nowhere must not be left in the resolution map.
        self.assertNotIn("01043b", data.ability_link)

    def test_ability_link_is_recorded_without_copying_anything(self):
        data = self.Load({"core": [
            MakeCard("01043a", name="Source", text="Source text."),
            MakeCard("01043b", name="Borrower", text="Own text.",
                     ability_link="01043a"),
        ]})
        self.assertEqual(data.ability_link["01043b"], "01043a")
        self.assertEqual(data.cards["01043b"].text, "Own text.")
        self.assertEqual(data.cards["01043b"].link_kind, "ability")

    def test_checksum_key_is_not_a_pack(self):
        data = self.Load({"core": [MakeCard("01002")], "checksum": "abc123"})
        self.assertEqual(list(data.cards), ["01002"])
        self.assertEqual(data.packs, ["core"])

    def test_expansion_labels_come_from_sets_info(self):
        data = self.Load(
            {"core": [MakeCard("01002")]},
            {"1. Core Set": {"name": "core"}, "checksum": "abc"},
        )
        self.assertEqual(data.expansions["core"], "1. Core Set")


class TestCleanName(unittest.TestCase):

    def test_matches_the_engine_on_every_set_name(self):
        """Cross-check the mirror against the real `FileManager.CleanName`.

        Skipped when the engine is not importable, which is the normal case in
        a bare checkout -- `tools/cards/` deliberately does not depend on it.
        """
        try:
            from engine.file.manager import FileManager
        except Exception as exc:  # pragma: no cover - depends on the environment
            self.skipTest(f"engine not importable: {exc}")

        raw = json.loads((REPO / engine.CARDS_JSON).read_text(encoding="utf-8"))
        names = {
            entry.get("set_name", "")
            for pack, entries in raw.items()
            if isinstance(entries, list)
            for entry in entries
        }
        self.assertGreater(len(names), 50)
        for name in sorted(names):
            self.assertEqual(engine.CleanName(name), FileManager.CleanName(name),
                             f"CleanName diverged on {name!r}")


class TestScriptAnalysis(unittest.TestCase):

    def Index(self, files):
        self._tmp = tempfile.TemporaryDirectory()
        root = Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)
        MakeScriptTree(root, files)
        return scripts.Index(root)

    def test_declarative_script_has_no_imperative_handler(self):
        index = self.Index({"core/01050.py": (
            "def GetAbilities():\n"
            "    return [AbilityFactory.Guard('This')]\n"
        )})
        facts = index.facts["cards/pack/core/01050.py"]
        self.assertFalse(facts.has_imperative_handler)
        self.assertEqual(facts.ability_factories, ["Guard"])

    def test_nested_function_is_an_imperative_handler(self):
        index = self.Index({"core/01051.py": (
            "def GetAbilities():\n"
            "    def handler(effect, message):\n"
            "        effect.this.Draw(1)\n"
            "    return [AbilityFactory.AfterPlayerPlayedCard(handler)]\n"
        )})
        facts = index.facts["cards/pack/core/01051.py"]
        self.assertTrue(facts.has_imperative_handler)
        self.assertEqual(facts.ability_factories, ["AfterPlayerPlayedCard"])

    def test_nested_handler_sharing_a_top_level_name_is_still_nested(self):
        # Nesting is parentage, not a name match. Getting this wrong moves a
        # card into the stratum that receives the least spec attention.
        index = self.Index({"core/01055.py": (
            "def GetAbilities():\n"
            "    def GetAbilities(effect, message):\n"
            "        effect.this.Draw(1)\n"
            "    return [AbilityFactory.Guard(GetAbilities)]\n"
        )})
        self.assertTrue(
            index.facts["cards/pack/core/01055.py"].has_imperative_handler)

    def test_deeply_nested_handler_is_detected(self):
        index = self.Index({"core/01056.py": (
            "def GetAbilities():\n"
            "    def outer(effect, message):\n"
            "        def inner(target):\n"
            "            target.Draw(1)\n"
            "        return inner\n"
            "    return []\n"
        )})
        self.assertTrue(
            index.facts["cards/pack/core/01056.py"].has_imperative_handler)

    def test_two_top_level_functions_alone_are_not_a_handler(self):
        index = self.Index({"core/01057.py": (
            "def Helper():\n"
            "    return 1\n"
            "def GetAbilities():\n"
            "    return [AbilityFactory.Guard('This')]\n"
        )})
        self.assertFalse(
            index.facts["cards/pack/core/01057.py"].has_imperative_handler)

    def test_player_choice_calls_are_detected(self):
        index = self.Index({"core/01052.py": (
            "def GetAbilities():\n"
            "    def handler(effect, message):\n"
            "        effect.GetInitiator().AskChooseFace(faces)\n"
            "        effect.GetInitiator().ChooseAbilities(effect)\n"
            "    return []\n"
        )})
        facts = index.facts["cards/pack/core/01052.py"]
        self.assertEqual(facts.player_choice_calls,
                         ["AskChooseFace", "ChooseAbilities"])

    def test_random_choice_is_not_player_choice(self):
        # `ChooseRandom` and friends draw from the seeded RNG. Counting them
        # would inflate the "suspends for a player" stratum with cards that
        # never stop for anyone.
        index = self.Index({"core/01053.py": (
            "def GetAbilities():\n"
            "    def handler(effect, message):\n"
            "        ModularSet.ChooseRandom(effect)\n"
            "        Rand.RandomChoice(items, effect)\n"
            "    return []\n"
        )})
        self.assertEqual(
            index.facts["cards/pack/core/01053.py"].player_choice_calls, [])

    def test_choice_api_is_derived_from_the_engine_source(self):
        index = self.Index({"core/01054.py": "def GetAbilities():\n    return []\n"})
        # From the stub `PlayerAsk`: prompts in, accessor and private out.
        self.assertIn("AskChooseFace", index.choice_api)
        self.assertIn("DiscardHandCards", index.choice_api)
        self.assertNotIn("GetPlayer", index.choice_api)
        self.assertNotIn("_Private", index.choice_api)
        # Plus the entry points that live on `PlayerAction`.
        self.assertIn("ChooseAbilities", index.choice_api)

    def test_missing_player_ask_source_fails_loudly(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(FileNotFoundError):
                scripts.Index(Path(tmp))


class TestScriptResolution(unittest.TestCase):

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        root = Path(self._tmp.name)
        body = "def GetAbilities():\n    return []\n"
        MakeScriptTree(root, {
            "core/01050.py": body,
            "core/spider_man/01002.py": body,
            "core/spider_man/09001.py": body,
            "gob/02019.py": body,
        })
        self.index = scripts.Index(root)

    def test_set_subdirectory_wins_over_the_pack_root(self):
        found = self.index.Resolve("01002", "core", "spider_man")
        self.assertEqual(found.path, "cards/pack/core/spider_man/01002.py")

    def test_falls_back_to_the_pack_root(self):
        found = self.index.Resolve("01050", "core", "she_hulk")
        self.assertEqual(found.path, "cards/pack/core/01050.py")

    def test_nemesis_set_falls_back_to_the_hero_directory(self):
        # `FindAbilities` strips the `_nemesis` suffix as a last resort, so a
        # nemesis card can live beside the hero's own cards.
        found = self.index.Resolve("09001", "core", "spider_man_nemesis")
        self.assertEqual(found.path, "cards/pack/core/spider_man/09001.py")

    def test_unresolvable_card_returns_none(self):
        self.assertIsNone(self.index.Resolve("99999", "core", "spider_man"))


class TestMarvelSdbLoader(unittest.TestCase):

    def Load(self, cards):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        root = Path(self._tmp.name)
        WriteJson(root / "pack/core.json", cards)
        for name, rows in (
            ("packs.json", [{"code": "core", "name": "Core Set"}]),
            ("sets.json", [{"code": "spider_man", "name": "Spider-Man"}]),
            ("types.json", [{"code": "ally", "name": "Ally"}]),
            ("factions.json", [{"code": "hero", "name": "Hero"}]),
            ("subtypes.json", []),
            ("settypes.json", []),
            ("packtypes.json", []),
        ):
            WriteJson(root / name, rows)
        return marvelsdb.Load(root)

    def Entry(self, code, **overrides):
        entry = {"code": code, "pack_code": "core", "position": 1, "quantity": 1}
        entry.update(overrides)
        return entry

    def test_traits_are_split_from_the_printed_string(self):
        self.assertEqual(marvelsdb.SplitTraits("Avenger. Genius."),
                         ["Avenger", "Genius"])
        self.assertEqual(marvelsdb.SplitTraits(""), [])

    def test_a_dotted_acronym_survives_the_split(self):
        """`S.H.I.E.L.D.` and `A.I.M.` are traits, not six and three of them.

        Splitting on every period turned 114 cards' trait line into `S`, `H`,
        `I`, `E`, `L`, `D`, which left Maria Hill's printed deck-building rule
        -- "3 [[S.H.I.E.L.D.]] supports" -- naming a trait the dataset did not
        have (MARVEL-85). The separator is a period *and a space*.
        """
        self.assertEqual(marvelsdb.SplitTraits("S.H.I.E.L.D."),
                         ["S.H.I.E.L.D."])
        self.assertEqual(marvelsdb.SplitTraits("Location. S.H.I.E.L.D."),
                         ["Location", "S.H.I.E.L.D."])
        self.assertEqual(marvelsdb.SplitTraits("S.H.I.E.L.D. Soldier. Spy."),
                         ["S.H.I.E.L.D.", "Soldier", "Spy"])
        self.assertEqual(marvelsdb.SplitTraits("A.I.M. Genius."),
                         ["A.I.M.", "Genius"])

    def test_unlisted_keys_become_stats(self):
        data = self.Load([self.Entry("01002", name="Black Cat", cost=2, attack=1,
                                     thwart_cost=1, illustrator="Someone")])
        card = data.cards["01002"]
        self.assertEqual(card.stats, {"attack": 1, "cost": 2, "thwart_cost": 1})
        # Identity keys stay off `stats` -- an illustrator is not a printed stat.
        self.assertEqual(card.illustrator, "Someone")

    def test_reprint_inherits_printed_content_but_keeps_its_own_printing(self):
        data = self.Load([
            self.Entry("01002", name="Black Cat", text="Discard 2.",
                       traits="Hero for Hire.", cost=2, set_code="spider_man"),
            self.Entry("42015", position=15, quantity=3, duplicate_of="01002"),
        ])
        reprint = data.cards["42015"]
        self.assertTrue(reprint.reprint)
        self.assertEqual(reprint.text, "Discard 2.")
        self.assertEqual(reprint.name, "Black Cat")
        self.assertEqual(reprint.traits, ["Hero for Hire"])
        self.assertEqual(reprint.stats, {"cost": 2})
        # Its own printing details are not the source's.
        self.assertEqual(reprint.position, 15)
        self.assertEqual(reprint.quantity, 3)

    def test_reprint_does_not_alias_the_source_collections(self):
        data = self.Load([
            self.Entry("01002", name="Black Cat", traits="Hero for Hire.", cost=2),
            self.Entry("42015", duplicate_of="01002"),
        ])
        data.cards["42015"].traits.append("Spy")
        data.cards["42015"].stats["cost"] = 9
        self.assertEqual(data.cards["01002"].traits, ["Hero for Hire"])
        self.assertEqual(data.cards["01002"].stats, {"cost": 2})

    def test_upstream_text_key_typo_is_used_and_reported(self):
        # Card 28022 upstream spells its printed text `scheme text`. Dropping a
        # card's only text would be worse than reading it and flagging it.
        data = self.Load([
            self.Entry("28022", name="Bring the War!",
                       **{"scheme text": "<b>When Revealed</b>: place 1 threat."}),
        ])
        self.assertEqual(data.cards["28022"].text,
                         "<b>When Revealed</b>: place 1 threat.")
        self.assertEqual(data.text_key_typos, ["28022"])

    def test_real_text_key_wins_over_the_typo(self):
        data = self.Load([
            self.Entry("28022", text="Real.", **{"scheme text": "Typo."}),
        ])
        self.assertEqual(data.cards["28022"].text, "Real.")
        self.assertEqual(data.text_key_typos, [])

    def test_dangling_duplicate_is_reported(self):
        data = self.Load([self.Entry("42015", duplicate_of="missing")])
        self.assertEqual(data.dangling_duplicates, [("42015", "missing")])
        self.assertFalse(data.cards["42015"].reprint)

    def test_incomplete_snapshot_fails_loudly(self):
        with tempfile.TemporaryDirectory() as tmp:
            with self.assertRaises(FileNotFoundError):
                marvelsdb.Load(Path(tmp))


class TestTextComparison(unittest.TestCase):

    def test_classifies_how_the_engine_copy_relates_to_the_printed_text(self):
        cases = [
            ("<b>Draw</b> 1.", "<b>Draw</b> 1.", "exact"),
            ("<b>Draw</b> 1.", "<b>Draw</b>  1.", "formatting"),
            ("<b>Draw</b> 1.", "Draw 1.", "formatting"),
            ("Max 1 per character. Draw 1.", "Draw 1.", "wording"),
            ("Draw 1.", "", "engine_missing"),
            ("", "Draw 1.", "marvelsdb_missing"),
            ("", "", None),
        ]
        for printed, known, expected in cases:
            with self.subTest(printed=printed, engine=known):
                self.assertEqual(extract._CompareText(printed, known), expected)


class TestAnomalyCollector(unittest.TestCase):

    def test_every_kind_is_documented(self):
        self.assertEqual(set(anomalies.KINDS), set(anomalies.DESCRIPTIONS))

    def test_undocumented_kind_is_rejected(self):
        found = anomalies.Collector()
        with self.assertRaises(KeyError):
            found.Add("something_new", "01002")

    def test_groups_cover_every_kind_and_sort_by_id(self):
        found = anomalies.Collector()
        found.Add("engine_text_corrupt", "05001b")
        found.Add("engine_text_corrupt", "05001a", "Ms. Marvel")
        groups = {g["kind"]: g for g in found.Grouped()}
        self.assertEqual(set(groups), set(anomalies.KINDS))
        corrupt = groups["engine_text_corrupt"]
        self.assertEqual(corrupt["count"], 2)
        self.assertEqual([c["id"] for c in corrupt["cards"]], ["05001a", "05001b"])
        self.assertEqual(found.Counts()["engine_text_corrupt"], 2)


class TestRealDataset(unittest.TestCase):
    """Guards the dataset that is actually checked in."""

    @classmethod
    def setUpClass(cls):
        if not (REPO / engine.CARDS_JSON).exists():
            raise unittest.SkipTest("run from py_src/ -- data/cards.json not found")
        if not SNAPSHOT.exists():
            raise unittest.SkipTest(f"vendored snapshot missing at {SNAPSHOT}")
        cls.outputs = extract.Build(REPO)
        cls.dataset = json.loads(cls.outputs[extract.CARDS_FILE])

    def test_build_is_reproducible(self):
        # The acceptance criterion: same inputs, same bytes. A dataset that
        # cannot be regenerated identically cannot be trusted to be reviewed.
        again = extract.Build(REPO)
        for name, content in self.outputs.items():
            with self.subTest(file=name):
                self.assertEqual(again[name], content)

    def test_checked_in_dataset_is_current(self):
        # `extract.Check` is the same code `--check` runs, so this test and the
        # CI gate cannot drift apart. Each entry is (file, verdict): `stale`
        # means regenerate, `line_endings` means the checkout rewrote the file
        # and the content is fine (MARVEL-73).
        stale = extract.Check(self.outputs, REPO / extract.OUTPUT_DIR)
        self.assertEqual(stale, [],
                         "regenerate with: python -m tools.cards.extract")

    def test_card_ids_are_unique_and_sorted(self):
        ids = [c["card_id"] for c in self.dataset["cards"]]
        self.assertEqual(ids, sorted(ids))
        self.assertEqual(len(ids), len(set(ids)))

    def test_every_record_has_the_same_shape(self):
        # A consumer that has to ask which keys a record has is reading a
        # different dataset per card.
        shape = set(self.dataset["cards"][0])
        for card in self.dataset["cards"]:
            with self.subTest(card=card["card_id"]):
                self.assertEqual(set(card), shape)

    def test_every_card_comes_from_at_least_one_source(self):
        for card in self.dataset["cards"]:
            with self.subTest(card=card["card_id"]):
                self.assertTrue(card["in_marvelsdb"] or card["in_engine"])

    def test_engine_only_cards_are_never_reported_as_agreeing(self):
        # There is nothing to agree with. Comparing the engine's text against
        # itself and calling it 'exact' would inflate the agreement tally with
        # cards that were never checked against anything.
        for card in self.dataset["cards"]:
            if card["in_marvelsdb"] or not card["engine"]:
                continue
            with self.subTest(card=card["card_id"]):
                self.assertIn(card["engine"]["text_comparison"],
                              ("marvelsdb_missing", None))

    def test_agreement_tally_covers_exactly_the_comparable_cards(self):
        summary = json.loads(self.outputs[extract.SUMMARY_FILE])
        tallied = sum(summary["engine_text_agreement"]["counts"].values())
        comparable = sum(
            1 for c in self.dataset["cards"]
            if c["engine"] and c["engine"]["text_comparison"] is not None
        )
        self.assertEqual(tallied, comparable)

    def test_printed_text_is_preferred_over_the_engine_copy(self):
        for card in self.dataset["cards"]:
            if card["in_marvelsdb"]:
                self.assertEqual(card["text_source"], "marvelsdb",
                                 f"{card['card_id']} did not use printed text")

    def test_printed_text_is_never_corrupt(self):
        # The whole reason MarvelSDB is vendored: the engine's copy has 36 cards
        # with a lost character, and specs must never be authored from those.
        corrupt = [c["card_id"] for c in self.dataset["cards"]
                   if c["in_marvelsdb"] and IsCorrupt(c["text"])]
        self.assertEqual(corrupt, [])

    def test_engine_text_corruption_is_reported_rather_than_hidden(self):
        found = json.loads(self.outputs[extract.ANOMALIES_FILE])
        counts = found["counts"]
        self.assertGreater(counts["engine_text_corrupt"], 0)
        reported = {
            c["id"] for g in found["groups"] if g["kind"] == "engine_text_corrupt"
            for c in g["cards"]
        }
        actual = {c["card_id"] for c in self.dataset["cards"]
                  if c["engine"] and IsCorrupt(c["engine"]["text"])}
        self.assertEqual(reported, actual)

    def test_scripts_resolve_to_files_that_exist(self):
        for card in self.dataset["cards"]:
            script = (card["engine"] or {}).get("script")
            if script:
                with self.subTest(card=card["card_id"]):
                    self.assertTrue((REPO / script["path"]).exists())

    def test_every_script_is_either_claimed_or_reported_unclaimed(self):
        index = scripts.Index(REPO)
        claimed = {c["engine"]["script"]["path"] for c in self.dataset["cards"]
                   if c["engine"] and c["engine"]["script"]}
        found = json.loads(self.outputs[extract.ANOMALIES_FILE])
        unclaimed = {c["id"] for g in found["groups"]
                     if g["kind"] == "unclaimed_script" for c in g["cards"]}
        self.assertEqual(claimed | unclaimed, set(index.facts))
        self.assertEqual(claimed & unclaimed, set())

    def test_summary_stratification_matches_the_records(self):
        summary = json.loads(self.outputs[extract.SUMMARY_FILE])
        strata = summary["stratification"]
        declarative = sum(
            1 for c in self.dataset["cards"]
            if c["engine"] and c["engine"]["script"]
            and not c["engine"]["script"]["has_imperative_handler"]
        )
        self.assertEqual(strata["no_imperative_handler"]["cards"], declarative)
        self.assertGreater(strata["suspends_for_player_choice"]["scripts"], 0)
        # The count is meaningless without the rule that produced it.
        self.assertTrue(strata["suspends_for_player_choice"]["api"])

    def test_pinned_commit_is_recorded_in_the_dataset(self):
        pinned = marvelsdb.ReadPinnedCommit(SNAPSHOT)
        self.assertEqual(len(pinned), 40)
        self.assertEqual(self.dataset["generated_from"]["marvelsdb_commit"], pinned)


if __name__ == "__main__":
    unittest.main()


class TestAbilityTypeMatchesItsEvent(unittest.TestCase):
    """A Boost-priority ability type may only sit on a defeat event.

    `AbilityType` carries two things at once: the words the UI prints, and the
    `TimingPriority` the ability resolves at. `WhenDefeated`, `WhenRevealed` and
    `Boost` all map to `TimingPriority.Boost`, which is the engine's encoding of
    the Rules Reference bundling those three at one level.

    Sibling Rivalry (18025) registered `AbilityType.WhenDefeated` on
    `AfterPhaseBegin`, a phase event that has nothing to do with being defeated.
    The card prints "Forced Response", so it resolved a priority level early and
    the UI named it wrongly (MARVEL-89).

    Nothing caught it. The dataset records each script's `ability_factories` but
    not the `AbilityType` passed to them, so the mistyping was invisible to
    every gate in the repo -- it was found by hand-building an (event, priority)
    index for MARVEL-83, and only because that pair had no other candidate.

    This is deliberately a narrow guard rather than a general trigger-word
    check. It asserts the one thing that can be read off the call site without
    resolving printed text, and today the corpus satisfies it exactly.
    """

    # `ResolveAbility` is a generic dispatcher rather than an event, so the
    # ability type there says what is being resolved, not when.
    DEFEAT_EVENTS = {"WhenUnitBeDefeated", "WhenSchemeBeDefeated", "ResolveAbility"}
    BOOST_PRIORITY_TYPES = {"WhenDefeated", "WhenRevealed", "Boost"}

    def test_no_boost_priority_type_sits_on_a_non_defeat_event(self):
        import ast

        root = Path(__file__).resolve().parents[1] / "cards"
        if not root.is_dir():
            self.skipTest("run from py_src/")

        offenders = []
        for path in sorted(root.rglob("*.py")):
            try:
                tree = ast.parse(path.read_text(encoding="utf-8"))
            except SyntaxError:
                continue
            for node in ast.walk(tree):
                if not (isinstance(node, ast.Call)
                        and isinstance(node.func, ast.Attribute)):
                    continue
                factory = node.func.attr
                if factory in self.DEFEAT_EVENTS:
                    continue
                for arg in node.args:
                    if (isinstance(arg, ast.Attribute)
                            and isinstance(arg.value, ast.Name)
                            and arg.value.id == "AbilityType"
                            and arg.attr in self.BOOST_PRIORITY_TYPES):
                        offenders.append(f"{path.name}: {arg.attr} on {factory}")

        self.assertEqual(
            offenders, [],
            "a Boost-priority AbilityType is registered on an event that is not "
            "about being defeated. Either the ability type is wrong (as in "
            "MARVEL-89) or this guard needs widening -- check the printed card.")


################################################################################
# The deck-building guard (MARVEL-88)
#

def Identity(card_id, text, *, card_set="hero_set", card_type="alter_ego",
             name="Somebody"):
    """The fields `tools.cards.deckbuilding` reads, and nothing else."""
    return {"card_id": card_id, "type": card_type, "set": card_set,
            "name": name, "text_plain": text, "back_text": ""}


# Cyclops', verbatim. The line the guard is pinned to.
CYCLOPS = "You may include [[X-MEN]] allies from any aspect in your deck."
CYCLOPS_HASH = "ba3d1502cd0af797"

# Nightcrawler's, verbatim: the net matches it, and it is reviewed as not a
# deck-building rule. His face prints exactly this one matching line.
NIGHTCRAWLER = ("Action: Search your deck for a copy of Bamf! and add it to "
                "your hand. (Limit once per round.)")


def Mentioning(result, card_id):
    """The problems about one card.

    A scan of a handful of synthetic records always reports the other 47 real
    lines as no longer printed, which is correct -- the tables describe the
    whole dataset -- and is noise when the question is about one card.
    """
    return [p for p in result.problems if p.startswith(card_id)]


class TestDeckbuildingTables(unittest.TestCase):
    """The parses, checked against the sentences they claim to come from."""

    def test_the_tables_are_internally_consistent(self):
        self.assertEqual(deckbuilding.SelfCheck(), [])

    def test_every_parse_matches_the_broad_net(self):
        """A parse of a line the net cannot see would never be reached."""
        for rule in deckbuilding.RULES:
            with self.subTest(card=rule.card_id):
                self.assertTrue(deckbuilding.Matches(rule.source_text))

    def test_a_cap_the_line_does_not_print_is_rejected(self):
        """Mutation. The number in the parse has to be the printed number.

        Nothing else in the pipeline contradicts a wrong cap: the parse is the
        only description of the rule there is, so a 7 read off a line that says
        6 would be believed, and Gamora's legal decks would be rejected.
        """
        gamora = next(r for r in deckbuilding.RULES if r.card_id == "18001b")
        wrong = dataclasses.replace(
            gamora,
            allowances=(dataclasses.replace(gamora.allowances[0], limit=7),))
        complaints = deckbuilding._ParseComplaints(wrong)
        self.assertTrue(complaints)
        self.assertIn("does not print the number 7", complaints[0])

    def test_a_trait_the_line_does_not_print_is_rejected(self):
        cyclops = next(r for r in deckbuilding.RULES if r.card_id == "33001b")
        wrong = dataclasses.replace(
            cyclops,
            allowances=(dataclasses.replace(cyclops.allowances[0],
                                            traits=("Avenger",)),))
        self.assertIn("[[Avenger]]",
                      " ".join(deckbuilding._ParseComplaints(wrong)))

    def test_a_card_type_the_line_does_not_print_is_rejected(self):
        cyclops = next(r for r in deckbuilding.RULES if r.card_id == "33001b")
        wrong = dataclasses.replace(
            cyclops,
            allowances=(dataclasses.replace(cyclops.allowances[0],
                                            card_type="upgrade"),))
        self.assertIn("'upgrades'",
                      " ".join(deckbuilding._ParseComplaints(wrong)))

    def test_no_line_is_both_a_rule_and_reviewed_as_not_one(self):
        rules = {(r.card_id, r.source_hash) for r in deckbuilding.RULES}
        reviewed = {(r.card_id, r.line_hash) for r in deckbuilding.REVIEWED}
        self.assertEqual(rules & reviewed, set())


class TestDeckbuildingGuard(unittest.TestCase):
    """The mechanism: a line nobody has classified stops the build.

    Every test here is a mutation of the real tables or the real text. An
    accept-only test would pass just as happily against a scan that classified
    nothing, which is the exact bug this replaces.
    """

    def test_a_reviewed_line_is_accepted(self):
        """Nightcrawler's, which the net matches and which is not a rule."""
        records = [Identity("48001b", NIGHTCRAWLER, card_set="nightcrawler")]
        result = deckbuilding.Scan(records)
        self.assertEqual(Mentioning(result, "48001b"), [])
        self.assertEqual(result.classified[("48001b",
                                            deckbuilding.LineHash(NIGHTCRAWLER))],
                         "search")

    def test_editing_a_reviewed_line_stops_it_being_accepted(self):
        """The reviewed set is pinned to the text, not to the card.

        Otherwise "somebody looked at this card once" would licence every later
        rewording of it, which is the failure the hash exists to prevent.
        """
        records = [Identity("48001b", NIGHTCRAWLER.replace("Bamf!", "Snikt!"),
                            card_set="nightcrawler")]
        self.assertTrue(Mentioning(deckbuilding.Scan(records), "48001b"))

    def test_a_new_deckbuilding_shaped_line_fails(self):
        """The point of the whole issue.

        A hero printed tomorrow whose rule nobody has read must stop the
        extract, not be silently checked as an ordinary one-aspect deck.
        """
        records = [Identity(
            "99001b",
            "You may include [[AVENGER]] upgrades from any aspect in your deck.",
            card_set="new_hero", name="New Hero")]
        result = deckbuilding.Scan(records)
        self.assertTrue(result.problems)
        self.assertIn("99001b", result.problems[0])
        self.assertIn("nobody has classified", result.problems[0])

    def test_the_failure_names_the_row_to_add(self):
        """The message is a work order, so the fix does not need archaeology."""
        records = [Identity("99001b", "Search your deck for a [[Tech]] upgrade.")]
        problem = deckbuilding.Scan(records).problems[0]
        self.assertIn('Reviewed("99001b"', problem)
        self.assertIn(deckbuilding.LineHash(
            "Search your deck for a [[Tech]] upgrade."), problem)

    def test_an_unclassified_line_raises_rather_than_warns(self):
        records = [Identity("99001b", "You may include anything in your deck.")]
        with self.assertRaises(deckbuilding.DeckbuildingError):
            deckbuilding.Apply(records)

    def test_a_line_the_net_does_not_match_is_ignored(self):
        """The limit of the guard, stated as a test rather than as a hope."""
        records = [Identity("99001b", "Hero Action: ready Somebody.")]
        result = deckbuilding.Scan(records)
        self.assertEqual(Mentioning(result, "99001b"), [])
        self.assertEqual(result.matched_lines, 0)

    def test_a_reworded_rule_fails_twice(self):
        """The pin. Change one character of a rule's line and it is not a rule.

        Both halves fire: the new wording is unclassified, and the parse now
        pins a sentence nothing prints. Either alone would be enough; both are
        reported because they are different repairs.
        """
        records = [Identity("33001b", CYCLOPS.replace("allies", "characters"),
                            card_set="cyclops", name="Cyclops")]
        problems = "\n".join(deckbuilding.Scan(records).problems)
        self.assertIn("nobody has classified", problems)
        self.assertIn("no longer printed on the card", problems)

    def test_a_rule_whose_card_vanishes_fails(self):
        problems = "\n".join(deckbuilding.Scan([]).problems)
        self.assertIn("33001b", problems)
        self.assertIn("no longer", problems)

    def test_a_reviewed_row_whose_line_vanishes_fails(self):
        """Stale allowlist rows are a failure too.

        A row nobody can trace back to a card is a row nobody will ever delete,
        and a table that accumulates them stops being auditable -- which is the
        only property it has.
        """
        problems = "\n".join(deckbuilding.Scan([]).problems)
        self.assertIn("is no longer printed", problems)

    def test_only_identity_faces_are_scanned(self):
        records = [Identity("99001", CYCLOPS, card_type="ally")]
        self.assertEqual(Mentioning(deckbuilding.Scan(records), "99001"), [])

    def test_an_inline_back_face_is_scanned_too(self):
        records = [{"card_id": "99001b", "type": "alter_ego", "set": "x",
                    "name": "X", "text_plain": "",
                    "back_text": "<b>Rule</b> — You may include anything in "
                                 "your deck."}]
        self.assertTrue(deckbuilding.Scan(records).problems)

    def test_the_hash_moves_when_the_line_does(self):
        self.assertEqual(deckbuilding.LineHash(CYCLOPS), CYCLOPS_HASH)
        self.assertNotEqual(deckbuilding.LineHash(CYCLOPS + "."), CYCLOPS_HASH)


class TestDeckbuildingAgainstTheRealDataset(unittest.TestCase):
    """The measurement in MARVEL-88, pinned."""

    @classmethod
    def setUpClass(cls):
        if not (REPO / engine.CARDS_JSON).exists():
            raise unittest.SkipTest("run from py_src/ -- data/cards.json not found")
        if not SNAPSHOT.exists():
            raise unittest.SkipTest(f"vendored snapshot missing at {SNAPSHOT}")
        cls.dataset = json.loads(extract.Build(REPO)[extract.CARDS_FILE])
        cls.records = cls.dataset["cards"]
        cls.result = deckbuilding.Scan(cls.records)

    def test_the_real_dataset_has_nothing_unclassified(self):
        self.assertEqual(self.result.problems, [])

    def test_the_broad_net_matches_what_the_issue_measured(self):
        """48 lines on 37 heroes, of which 7 are rules and 41 are not.

        Pinned as a number because the argument for the reviewed set rests on
        it: a net precise enough not to need one does not exist, and if these
        counts move a long way somebody should re-read the reasoning.
        """
        matched = list(deckbuilding.Iterate(self.records))
        heroes = {c["set"] for c in self.records
                  if c["card_id"] in {card_id for card_id, _ in matched}}
        self.assertEqual(len(matched), 48)
        self.assertEqual(len(heroes), 37)
        kinds = self.result.classified
        self.assertEqual(sum(1 for k in kinds.values() if k == "rule"), 7)
        self.assertEqual(sum(1 for k in kinds.values() if k != "rule"), 41)

    def test_all_seven_printed_rules_are_found(self):
        found = {block["source_card"] for block in self.result.blocks.values()}
        self.assertEqual(found, {"04031b", "18001b", "21031b", "33001b",
                                 "40001b", "50001b", "58001b"})

    def test_every_source_text_is_a_line_the_card_actually_prints(self):
        """`source_text` is carried so a human can audit the parse in place.

        It is worth nothing if it is a paraphrase, so it is checked against the
        card rather than trusted.
        """
        by_id = {c["card_id"]: c for c in self.records}
        for rule in deckbuilding.RULES:
            with self.subTest(card=rule.card_id):
                self.assertIn(rule.source_text,
                              deckbuilding.Lines(by_id[rule.card_id]))

    def test_the_block_is_emitted_on_every_face_of_a_hero_that_has_one(self):
        sets = {c["set"] for c in self.records if c.get("deckbuilding")}
        for hero_set in sets:
            faces = [c for c in self.records
                     if c.get("type") in deckbuilding.IDENTITY_TYPES
                     and c["set"] == hero_set]
            with self.subTest(hero=hero_set):
                self.assertTrue(faces)
                self.assertTrue(all(c.get("deckbuilding") for c in faces))

    def test_a_perturbed_card_fails_the_build(self):
        """Mutation, end to end: change the printed text, watch `Build` refuse.

        This is the acceptance criterion of MARVEL-88 written as a test. It
        perturbs the records the extract would write rather than the vendored
        snapshot, which is the same input at the only point that matters.
        """
        records = [dict(c) for c in self.records]
        for record in records:
            if record["card_id"] == "33001b":
                record["text_plain"] = record["text_plain"].replace(
                    "[[X-MEN]] allies", "[[X-MEN]] characters")
        with self.assertRaises(deckbuilding.DeckbuildingError) as caught:
            deckbuilding.Apply(records)
        self.assertIn("33001b", str(caught.exception))

    def test_removing_the_guard_would_be_caught(self):
        """The guard cannot be quietly emptied.

        A `REVIEWED` table pruned to nothing, or a `BROAD_NET` narrowed to
        match nothing, both leave a scan that classifies nothing and reports no
        problem -- which is indistinguishable from a clean run unless somebody
        asserts the work was done.
        """
        self.assertEqual(self.result.matched_lines, 48)
        self.assertEqual(len(deckbuilding.REVIEWED), 41)
        self.assertEqual(len(deckbuilding.RULES), 7)
