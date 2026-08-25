"""Build the spec-authoring dataset in `datasets/cards/` (MARVEL-19).

Joins the vendored MarvelSDB snapshot (authoritative printed text), the Python
engine's `data/cards.json` (what the implementation believes) and the card
scripts under `cards/pack/` (what the implementation does), then writes three
files:

    datasets/cards/cards.json       one record per card, union of both sources
    datasets/cards/anomalies.json   every place the sources disagree or fall short
    datasets/cards/summary.json     counts, inventories and the rules behind them

Run from `py_src/`:

    python -m tools.cards.extract           # write the dataset
    python -m tools.cards.extract --check    # fail if the checked-in copy is stale

`--check` is what makes "generated reproducibly" a claim you can test rather
than a hope. It regenerates into memory and compares byte for byte; nothing is
written. What that comparison forgives and what it does not is decided once, in
`tools/fixtures.py`, for this gate and the two vector gates alike.

Both modes **fail** when an identity prints a deck-building line that nobody
has classified -- see `tools/cards/deckbuilding.py`. That is deliberate: a new
hero whose printed rule went unnoticed is a hero whose legal decks get called
illegal, and the failure has to happen at the build rather than months later.

The output is deterministic by construction: cards sorted by id, dict keys in a
fixed order, every collection sorted before it is written, and no wall-clock
anywhere. Regenerating without changing an input produces byte-identical files.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any, Dict, List, Tuple

from tools import fixtures
from tools.cards import anomalies as anomaly_module
from tools.cards import deckbuilding as deckbuilding_module
from tools.cards import engine as engine_module
from tools.cards import marvelsdb as marvelsdb_module
from tools.cards import scripts as scripts_module
from tools.cards.text import IsCorrupt, ToPlainText

DATASET_VERSION = 1

SNAPSHOT_DIR = Path("../datasets/marvelsdb")
OUTPUT_DIR = Path("../datasets/cards")

CARDS_FILE = "cards.json"
ANOMALIES_FILE = "anomalies.json"
SUMMARY_FILE = "summary.json"

# Escaped-slash markup: `<\/b>`. No renderer reads it as a closing tag.
ESCAPED_MARKUP = "<\\/"


def TraitKey(engine_trait: str) -> str:
    """An engine trait as the digest keys it, without the `t_` prefix.

    `CardFace.GetInfoTraits` builds every key as
    `f"t_{trait.replace(' ', '_').replace('!', '')}"` over the engine's own
    trait list. Two traits carry the `!`: `CHASE!` and `TRAP!`, on five cards
    between them.

    The engine's traits are already upper-case and already have no trailing
    stop -- `A.I.M` and `S.H.I.E.L.D` are stored that way. So a port reading
    this list needs only these two substitutions, and a port deriving keys from
    the *printed* traits instead needs upper-casing and stop-trimming as well,
    and still gets `t_TRAP!` wrong.
    """
    return engine_trait.replace(" ", "_").replace("!", "")


def PrintedTraitKey(printed_trait: str) -> str:
    """A printed trait rendered the way the engine would spell it.

    Only for *comparing* the two lists, so that a difference reported here is a
    difference about the card rather than about punctuation. MarvelSDB prints
    `Trap!`, `S.H.I.E.L.D.` and `Hero for Hire`; the engine stores `TRAP!`,
    `S.H.I.E.L.D` and `HERO FOR HIRE`, and keys all three without the `!`, the
    trailing stop or the spaces.

    **Not how the digest is built, and a port must not use it.** It agrees with
    `TraitKey` on all but twelve of 3,999 cards, which is exactly often enough
    to look right -- and those twelve are the point.
    """
    return TraitKey(printed_trait.upper().rstrip("."))


# --------------------------------------------------------------------------
# Building one record
# --------------------------------------------------------------------------

def _CompareText(printed: str, engine_text: str) -> str | None:
    """How the engine's copy of a card's text relates to the printed text."""
    if not printed and not engine_text:
        return None
    if not printed:
        return "marvelsdb_missing"
    if not engine_text:
        return "engine_missing"
    if printed == engine_text:
        return "exact"
    if ToPlainText(printed) == ToPlainText(engine_text):
        return "formatting"
    return "wording"


def _ScriptRecord(facts: scripts_module.ScriptFacts) -> Dict[str, Any]:
    return {
        "path": facts.path,
        "lines": facts.lines,
        "has_imperative_handler": facts.has_imperative_handler,
        "player_choice_calls": facts.player_choice_calls,
        "player_choice_helpers": facts.player_choice_helpers,
        "ability_factories": facts.ability_factories,
    }


def _EngineRecord(
    card: engine_module.SourceCard,
    index: scripts_module.ScriptIndex,
    source: engine_module.SourceData,
    printed: str,
) -> Dict[str, Any]:
    # `FindAbilities` follows one hop before looking for a module: an
    # `ability_link` card borrows the linked card's script outright, and a
    # reprint resolves through the card it copies.
    target_id = source.ability_link.get(card.card_id,
                                        source.full_link.get(card.card_id, card.card_id))
    target = source.cards.get(target_id, card)
    facts = index.Resolve(target_id, target.pack,
                          engine_module.CleanName(target.set_name))

    link: Dict[str, str] | None = None
    if card.link_kind:
        link = {"kind": card.link_kind, "card_id": card.link_target or ""}

    return {
        "pack": card.pack,
        "set_name": card.set_name,
        "type": card.type,
        # The list the *digest* is built from. `traits` at the top level is
        # MarvelSDB's printed spelling and is not the same list -- see
        # `TraitKey` below and `docs/card-dataset.md`.
        "traits": list(card.traits),
        "attributes": card.attributes,
        "text": card.text,
        "text_comparison": _CompareText(printed, card.text),
        "link": link,
        "script": _ScriptRecord(facts) if facts else None,
    }


def _Record(
    card_id: str,
    printed: marvelsdb_module.MarvelCard | None,
    known: engine_module.SourceCard | None,
    index: scripts_module.ScriptIndex,
    source: engine_module.SourceData,
    reference: marvelsdb_module.MarvelData,
) -> Dict[str, Any]:
    """One dataset record. Every field is always present; absence is a value.

    Printed text and everything printed alongside it comes from MarvelSDB when
    it has the card. The engine's own view lives under `engine`, so which side
    a fact came from is never a guess.
    """
    # Kept separate from `text` on purpose. `text` is what the dataset serves,
    # which falls back to the engine's copy when MarvelSDB has no such card;
    # `printed_text` is what there is to check the engine against, and there is
    # nothing when the card is engine-only. Collapsing the two would have the
    # engine's text compared against itself and reported as agreeing.
    printed_text = printed.text if printed is not None else ""

    if printed is not None:
        text = printed.text
        text_source = "marvelsdb"
    elif known is not None:
        text = known.text
        text_source = "engine" if text else "none"
    else:  # unreachable -- the union only contains ids one source has
        text, text_source = "", "none"

    if printed is not None:
        identity: Dict[str, Any] = {
            "name": printed.name,
            "subname": printed.subname,
            "unique": printed.is_unique,
            "type": printed.type_code,
            "type_name": reference.type_names.get(printed.type_code, ""),
            "faction": printed.faction_code,
            "faction_name": reference.faction_names.get(printed.faction_code, ""),
            "traits": printed.traits,
            "flavor": printed.flavor,
            "errata": printed.errata,
            "stats": printed.stats,
            "pack": printed.pack_code,
            "pack_name": reference.pack_names.get(printed.pack_code, ""),
            "set": printed.set_code,
            "set_name": reference.set_names.get(printed.set_code, ""),
            "position": printed.position,
            "quantity": printed.quantity,
            "deck_limit": printed.deck_limit,
            "hidden": printed.hidden,
            "reprint_of": printed.duplicate_of or None,
            "back_link": printed.back_link or None,
            "back_name": printed.back_name,
            "back_text": printed.back_text,
        }
    else:
        # Engine-only: the engine's vocabulary is all there is. Kept in the same
        # field shape so a consumer never has to branch on which keys exist.
        assert known is not None
        identity = {
            "name": known.name,
            "subname": known.subtitle,
            "unique": known.unique,
            "type": known.type,
            "type_name": known.type,
            "faction": known.attributes.get("Class", ""),
            "faction_name": known.attributes.get("Class", ""),
            "traits": known.traits,
            "flavor": "",
            "errata": "",
            "stats": {},
            "pack": known.pack,
            "pack_name": source.expansions.get(known.pack, ""),
            "set": engine_module.CleanName(known.set_name),
            "set_name": known.set_name,
            "position": None,
            "quantity": None,
            "deck_limit": None,
            "hidden": False,
            "reprint_of": known.link_target if known.link_kind == "full" else None,
            "back_link": None,
            "back_name": "",
            "back_text": "",
        }

    record: Dict[str, Any] = {
        "card_id": card_id,
        "in_marvelsdb": printed is not None,
        "in_engine": known is not None,
        "text": text,
        "text_plain": ToPlainText(text),
        "text_source": text_source,
    }
    record.update(identity)
    # Filled in by `tools.cards.deckbuilding` once every record exists, because
    # the rule is printed on one face and applies to the identity's whole set.
    # Declared here so the key order does not depend on which cards have one.
    record["deckbuilding"] = None
    record["engine"] = (
        _EngineRecord(known, index, source, printed_text) if known is not None else None
    )
    return record


# --------------------------------------------------------------------------
# Anomalies
# --------------------------------------------------------------------------

def _CollectAnomalies(
    records: List[Dict[str, Any]],
    source: engine_module.SourceData,
    reference: marvelsdb_module.MarvelData,
    index: scripts_module.ScriptIndex,
    claimed_scripts: set[str],
) -> anomaly_module.Collector:
    found = anomaly_module.Collector()

    for record in records:
        card_id = record["card_id"]
        known = record["engine"]
        has_text = bool(record["text"].strip())
        has_script = bool(known and known["script"])

        if known is not None:
            printed_keys = sorted({PrintedTraitKey(trait) for trait in record["traits"]})
            engine_keys = sorted({TraitKey(trait) for trait in known["traits"]})
            if printed_keys != engine_keys:
                found.Add("engine_traits_diverge", card_id,
                          f"{record['name']}: printed {printed_keys or '[]'} "
                          f"vs engine {engine_keys or '[]'}")

            comparison = known["text_comparison"]
            if comparison == "wording":
                found.Add("engine_text_diverges", card_id, record["name"])
            elif comparison == "engine_missing":
                found.Add("engine_text_missing", card_id, record["name"])
            if IsCorrupt(known["text"]):
                found.Add("engine_text_corrupt", card_id, record["name"])
            if ESCAPED_MARKUP in known["text"]:
                found.Add("engine_markup_escaped", card_id, record["name"])

        if not record["in_marvelsdb"]:
            found.Add("card_not_in_marvelsdb", card_id, record["name"])
        elif not has_script:
            detail = record["name"]
            if record["reprint_of"]:
                detail = f"{record['name']} (reprint of {record['reprint_of']})"
            found.Add("card_not_implemented", card_id, detail)

        if has_script and not has_text:
            found.Add("script_without_text", card_id, known["script"]["path"])
        elif not has_script and not has_text:
            found.Add("no_text_anywhere", card_id, record["name"])

    for path in sorted(set(index.facts) - claimed_scripts):
        found.Add("unclaimed_script", path)

    for pack in sorted(set(source.packs) - set(source.expansions)):
        found.Add("engine_pack_without_expansion", pack or "(unnamed)")

    for card_id, pack in sorted(source.duplicate_ids):
        found.Add("engine_duplicate_card_id", card_id, f"second entry in pack {pack!r}")

    for card_id, kind, target in sorted(source.dangling_links):
        found.Add("engine_dangling_link", card_id, f"{kind}_link -> {target}")

    for code in sorted(reference.text_key_typos):
        found.Add("upstream_text_key_typo", code, "printed text found under 'scheme text'")

    for code in sorted(reference.duplicate_codes):
        found.Add("upstream_duplicate_code", code)

    for code, target in sorted(reference.dangling_duplicates):
        found.Add("upstream_dangling_duplicate", code, f"duplicate_of -> {target}")

    return found


# --------------------------------------------------------------------------
# Summary
# --------------------------------------------------------------------------

def _Tally(values: Any) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    for value in values:
        key = str(value)
        counts[key] = counts.get(key, 0) + 1
    return dict(sorted(counts.items(), key=lambda kv: (-kv[1], kv[0])))


def _Summary(
    records: List[Dict[str, Any]],
    source: engine_module.SourceData,
    reference: marvelsdb_module.MarvelData,
    index: scripts_module.ScriptIndex,
    found: anomaly_module.Collector,
    claimed_scripts: set[str],
) -> Dict[str, Any]:
    both = [r for r in records if r["in_marvelsdb"] and r["in_engine"]]
    engine_only = [r for r in records if not r["in_marvelsdb"]]
    marvelsdb_only = [r for r in records if not r["in_engine"]]

    comparisons = _Tally(
        r["engine"]["text_comparison"] for r in records
        if r["engine"] and r["engine"]["text_comparison"]
    )

    # The script/text cross-tab: the four-way split that says how much of the
    # card pool is actually ready to have specs written against it.
    cross_tab: Dict[str, int] = {}
    for record in records:
        has_script = bool(record["engine"] and record["engine"]["script"])
        has_text = bool(record["text"].strip())
        key = f"{'script' if has_script else 'no_script'}_{'text' if has_text else 'no_text'}"
        cross_tab[key] = cross_tab.get(key, 0) + 1

    scripts_claimed = [index.facts[p] for p in sorted(claimed_scripts)]
    factories: Dict[str, int] = {}
    for facts in index.facts.values():
        for name in facts.ability_factories:
            factories[name] = factories.get(name, 0) + 1

    attribute_keys: Dict[str, int] = {}
    for card in source.cards.values():
        for key in card.attributes:
            attribute_keys[key] = attribute_keys.get(key, 0) + 1

    stat_keys: Dict[str, int] = {}
    for card in reference.cards.values():
        for key in card.stats:
            stat_keys[key] = stat_keys.get(key, 0) + 1

    return {
        "dataset_version": DATASET_VERSION,
        "totals": {
            "cards": len(records),
            "in_both_sources": len(both),
            "engine_only": len(engine_only),
            "marvelsdb_only": len(marvelsdb_only),
            "with_printed_text": sum(1 for r in records if r["text"].strip()),
            "with_script": sum(
                1 for r in records if r["engine"] and r["engine"]["script"]
            ),
        },
        "engine_text_agreement": {
            "rule": (
                "Engine text compared against the printed text. 'exact' is byte "
                "equality; 'formatting' means equal after HTML tags, entities and "
                "whitespace are normalised; 'wording' means the words differ."
            ),
            "counts": comparisons,
        },
        "script_text_cross_tab": dict(sorted(cross_tab.items())),
        "stratification": {
            "no_imperative_handler": {
                "rule": (
                    "Card scripts whose syntax tree contains no function defined "
                    "inside another -- purely declarative AbilityFactory calls, so "
                    "the least spec attention is needed."
                ),
                "scripts": sum(
                    1 for f in index.facts.values() if not f.has_imperative_handler
                ),
                "cards": sum(
                    1 for r in records
                    if r["engine"] and r["engine"]["script"]
                    and not r["engine"]["script"]["has_imperative_handler"]
                ),
            },
            "suspends_for_player_choice": {
                "rule": (
                    "Card scripts calling a method of PlayerAsk "
                    "(game/player/model/player_ask.py) or one of ChooseAbilities / "
                    "MayChooseOneAbility / AskSpendResources "
                    "(game/player/action/player_action.py). These suspend "
                    "mid-resolution for a player answer and deserve the most spec "
                    "attention. Random-choice helpers are excluded -- they suspend "
                    "nothing."
                ),
                "api": index.choice_api,
                "scripts": sum(
                    1 for f in index.facts.values() if f.player_choice_calls
                ),
                "cards": sum(
                    1 for r in records
                    if r["engine"] and r["engine"]["script"]
                    and r["engine"]["script"]["player_choice_calls"]
                ),
            },
            "suspends_through_a_helper": {
                "rule": (
                    "Card scripts calling a game/operate/ helper which, with "
                    "the arguments passed at that call site, reaches a prompt "
                    "on every path through it (MARVEL-114). "
                    "cards_that_name_no_prompt_themselves is the subset whose "
                    "only question is asked inside the helper -- the population "
                    "the depth tier was getting wrong. Deliberately an "
                    "under-approximation: helpers "
                    "whose prompt depends on board state -- Faces.DiscardAll "
                    "under simultaneous=True, Worlds.FindMainScheme when more "
                    "than one main scheme is in play -- are not counted, "
                    "because a false 'this card asks' is as wrong in this "
                    "dataset as a false 'it does not'. The rule is in "
                    "tools/cards/helper_prompts.py."
                ),
                "helpers": sorted({
                    helper for f in index.facts.values()
                    for helper in f.player_choice_helpers
                }),
                "scripts": sum(
                    1 for f in index.facts.values() if f.player_choice_helpers
                ),
                "cards": sum(
                    1 for r in records
                    if r["engine"] and r["engine"]["script"]
                    and r["engine"]["script"]["player_choice_helpers"]
                ),
                "cards_that_name_no_prompt_themselves": sum(
                    1 for r in records
                    if r["engine"] and r["engine"]["script"]
                    and r["engine"]["script"]["player_choice_helpers"]
                    and not r["engine"]["script"]["player_choice_calls"]
                ),
            },
        },
        "scripts": {
            "files": len(index.facts),
            "claimed_by_a_card": len(scripts_claimed),
            "unclaimed": len(index.facts) - len(scripts_claimed),
            "total_lines": sum(f.lines for f in index.facts.values()),
            "distinct_ability_factories": len(factories),
            "ability_factories": dict(
                sorted(factories.items(), key=lambda kv: (-kv[1], kv[0]))
            ),
        },
        "by_type": _Tally(r["type"] for r in records),
        "by_pack": _Tally(r["pack"] for r in records),
        "marvelsdb_stat_keys": dict(
            sorted(stat_keys.items(), key=lambda kv: (-kv[1], kv[0]))
        ),
        "engine_attribute_keys": dict(
            sorted(attribute_keys.items(), key=lambda kv: (-kv[1], kv[0]))
        ),
        "anomalies": found.Counts(),
    }


# --------------------------------------------------------------------------
# Build and write
# --------------------------------------------------------------------------

def Build(root: Path = Path(".")) -> Dict[str, str]:
    """Produce the three output files as text. Nothing touches the disk here."""
    source = engine_module.Load(root)
    reference = marvelsdb_module.Load(root / SNAPSHOT_DIR)
    index = scripts_module.Index(root)

    card_ids = sorted(set(source.cards) | set(reference.cards))
    records = [
        _Record(card_id, reference.cards.get(card_id), source.cards.get(card_id),
                index, source, reference)
        for card_id in card_ids
    ]

    # Raises rather than returns when an identity prints a deck-building line
    # nobody has classified. Before the records are rendered, so a dataset that
    # would silently drop a rule never reaches the disk.
    deckbuilding = deckbuilding_module.Apply(records)

    claimed_scripts = {
        r["engine"]["script"]["path"]
        for r in records if r["engine"] and r["engine"]["script"]
    }

    found = _CollectAnomalies(records, source, reference, index, claimed_scripts)
    summary = _Summary(records, source, reference, index, found, claimed_scripts)
    summary["deckbuilding"] = deckbuilding_module.SummaryOf(deckbuilding)

    header = {
        "dataset_version": DATASET_VERSION,
        "generated_from": {
            "marvelsdb_commit": marvelsdb_module.ReadPinnedCommit(root / SNAPSHOT_DIR),
            "engine_files": dict(sorted(source.checksums.items())),
        },
        "counts": summary["totals"],
        "notes": (
            "Printed text is MarvelSDB's; 'engine' holds what the Python engine "
            "believes and which script implements the card. Where they disagree, "
            "the printed text wins -- see datasets/cards/anomalies.json."
        ),
    }

    return {
        CARDS_FILE: _RenderCards(header, records),
        ANOMALIES_FILE: _Render({
            "dataset_version": DATASET_VERSION,
            "counts": found.Counts(),
            "groups": found.Grouped(),
        }),
        SUMMARY_FILE: _Render(summary),
    }


def _Render(payload: Any) -> str:
    return json.dumps(payload, ensure_ascii=False, indent=2) + "\n"


def _RenderCards(header: Dict[str, Any], records: List[Dict[str, Any]]) -> str:
    """Header pretty-printed, then one card per line.

    A card per line is the point: `git diff` on a regenerated dataset shows the
    cards that changed, not a reflowed 100,000-line blob. Worth hand-rolling,
    because no `json.dumps` indent setting produces it.
    """
    fields = [
        f'  {json.dumps(key)}: '
        + json.dumps(value, ensure_ascii=False, indent=2).replace("\n", "\n  ")
        for key, value in header.items()
    ]
    fields.append('  "cards": [\n' + ",\n".join(
        "    " + json.dumps(record, ensure_ascii=False, separators=(",", ":"))
        for record in records
    ) + "\n  ]")

    rendered = "{\n" + ",\n".join(fields) + "\n}\n"
    json.loads(rendered)  # a malformed dataset must never reach the disk
    return rendered


def Write(outputs: Dict[str, str], directory: Path) -> None:
    directory.mkdir(parents=True, exist_ok=True)
    for name, content in outputs.items():
        (directory / name).write_text(content, encoding="utf-8", newline="\n")


def Check(outputs: Dict[str, str], directory: Path) -> List[Tuple[str, str]]:
    """(file, verdict) for every checked-in copy that is not a fresh build.

    Empty when the dataset on disk is byte for byte what `Build` produced.
    `tools/fixtures.py` owns both the verdicts and the decision to compare
    bytes, and is the one place all three fixture gates take their meaning of
    "stale" from. Bytes matter more here than anywhere else in the repo:
    `_RenderCards` hand-rolls the layout so a `git diff` shows the cards that
    changed, and a comparison that ignored the layout would let the property
    the layout exists for rot unnoticed.
    """
    stale: List[Tuple[str, str]] = []
    for name, content in outputs.items():
        verdict = fixtures.Compare(content, directory / name)
        if verdict != fixtures.FRESH:
            stale.append((name, verdict))
    return stale


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true",
                        help="compare against the checked-in dataset instead of "
                             "writing; exit 1 if it is stale")
    parser.add_argument("--out", type=Path, default=OUTPUT_DIR,
                        help=f"output directory (default {OUTPUT_DIR})")
    args = parser.parse_args(argv)

    try:
        outputs = Build()
    except deckbuilding_module.DeckbuildingError as error:
        # Printed rather than raised: the message is a work order for a human,
        # and a traceback in front of it only buries the card ids.
        print(error, file=sys.stderr)
        return 1
    summary = json.loads(outputs[SUMMARY_FILE])

    if args.check:
        stale = Check(outputs, args.out)
        if stale:
            print("dataset does not match a fresh build: " + ", ".join(
                f"{name} ({fixtures.SUMMARY[verdict]})" for name, verdict in stale))
            # One explanation per distinct verdict, against the first file that
            # earned it: the repair for CRLF is not "regenerate", and printing
            # that would send the next Windows contributor down the road
            # MARVEL-73 had to walk back.
            explained: set[str] = set()
            for name, verdict in stale:
                if verdict not in explained:
                    explained.add(verdict)
                    print(fixtures.Explain(verdict, args.out / name,
                                           "python -m tools.cards.extract"))
            return 1
        print(f"dataset up to date ({summary['totals']['cards']} cards)")
        return 0

    Write(outputs, args.out)
    totals = summary["totals"]
    print(f"wrote {args.out}/")
    print(f"  {totals['cards']} cards "
          f"({totals['in_both_sources']} in both sources, "
          f"{totals['engine_only']} engine-only, "
          f"{totals['marvelsdb_only']} MarvelSDB-only)")
    print(f"  {totals['with_printed_text']} with printed text, "
          f"{totals['with_script']} with a script")
    flagged = sum(summary["anomalies"].values())
    print(f"  {flagged} anomalies across "
          f"{sum(1 for n in summary['anomalies'].values() if n)} kinds")
    rules = summary["deckbuilding"]
    print(f"  {rules['parsed_rules']} deck-building rules parsed, "
          f"{rules['reviewed_lines']} lines reviewed and not one "
          f"({rules['matched_lines']} identity lines matched)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
