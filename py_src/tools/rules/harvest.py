"""Harvest the Rules Reference PDF into a citable, readable rules index.

    python -m tools.rules.harvest                 # write the snapshot
    python -m tools.rules.harvest --check         # is the committed snapshot current?
    python -m tools.rules.harvest --pdf PATH      # a Rules Reference somewhere else

Run from `py_src/`. Needs `pdfplumber`, which is deliberately **not** in
`requirements.lock` -- see "Why this is not a CI gate" below.

## What this produces, and why it is two artefacts

`docs/rules-provenance.md` asks for a citation index. Working with it made a
second requirement obvious: the thing a *spec* pins to and the thing an *agent*
reads are not the same artefact, and trying to make one serve both makes it bad
at each.

    datasets/rules-reference/
      index.json        every citable unit: id, path, fragment, hash
      icons.json        the glyph legend, derived from the document
      entries/*.md      one linked markdown document per entry

**`index.json` is for machines.** One record per citable unit, carrying a
one-sentence `fragment` and a `hash` over the normative text. `fragment` is
short on purpose: it exists to make a citation legible in a diff and to let
`tools.rules.diff` say what moved. Nobody adjudicates from it.

**`entries/*.md` is for agents and humans.** The full normative text of each
entry, with its clauses anchored so a citation resolves to a place in a
document you can actually read, and with `See also` rendered as real links so
the corpus can be followed rather than searched. An agent asking "can this
already-exhausted ally be exhausted to pay for that" needs the rule, not a
summary of it.

Both are generated from one parse, so they cannot disagree, and both are
covered by the same hashes.

## Why this is not a CI gate

Every other fixture in this repository is a regenerate-or-fail check
(`tools.rng.emit_vectors --check` and friends, wired into `ci.yml`). This one
cannot be. The source is 353 MB of PDFs under `~/Documents/Marvel Champions
LCG` which are copyrighted, are not in the repository, and are not going to be.
CI has nothing to regenerate from.

So this follows `tools.cards.harvest_faq`: the result is *vendored*, not
*generated*. `--check` exists for the person holding the PDFs, to answer "is
what I committed still what the document says" before a refresh lands. What CI
verifies is the snapshot's internal consistency -- see
`unit_test/test_rules_index.py`.

`pdfplumber` stays out of `requirements.lock` for the same reason the FAQ
harvester shells out to a CLI instead of speaking HTTP: nothing on the build
path needs it, and a dependency that only one local tool imports should not be
installed for every test run on every OS.

## Scope: this is the `rr:` tier only

MARVEL-154 defines two id tiers. This module implements the first:

    rr:<entry>[.<clause>[.<sub>]]     Rules Reference v1.8 -- the authority
    pack:<mcNN>:<section>            per-pack rules -- not yet implemented

The 61 rulesheets, inserts and expansion rulebooks are prose about one hero or
one scenario and have no shared structure to parse; they need a different
reader, and they are where new keywords arrive. That is the second leg.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
from typing import Dict, List, Sequence

from tools.rules.geometry import page_lines
from tools.rules.parse import Clause, Entry, is_banner, parse_entries

DEFAULT_PDF = os.path.expanduser(
    "~/Documents/Marvel Champions LCG/mc_rulesreference_v18_compressed.pdf")

OUT_DIR = os.path.join("..", "datasets", "rules-reference")

RR_VERSION = "1.8"

# The glossary runs from the OVERVIEW section to the first appendix. Both ends
# are found by content rather than hardcoded, but the search is bounded so a
# stray heading in the appendices cannot silently extend it.
GLOSSARY_FIRST_PAGE = 4
GLOSSARY_LAST_PAGE = 49

# Page furniture: a bare folio, and the running header, which extracts with
# stray spaces because it is letter-spaced.
FOLIO = re.compile(r'^\d{1,3}$')


def _is_furniture(text: str) -> bool:
    if FOLIO.match(text):
        return True
    squashed = text.replace(" ", "").lower()
    return squashed.startswith("rulesreference") and len(squashed) < 20


def slug(title: str) -> str:
    """A stable, filesystem-safe id fragment for an entry title."""
    text = title.replace("’", "'").replace("“", '"').replace("”", '"')
    # Icon tokens go, and the parentheses left holding nothing go with them.
    # Parentheses carrying *words* stay: they are the document disambiguating
    # two entries with the same name, and dropping them collides
    # `ATTACK (ENEMY ACTIVATION)` with `ATTACK (PLAYER ABILITY TYPE)`.
    text = re.sub(r'\[[^\]]*\]', ' ', text)
    text = re.sub(r'\(\s*\)', ' ', text)
    text = text.lower()
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")


def icon_legend(pdf) -> Dict[str, str]:
    """Map each private-use glyph to a name, using the document's own headings.

    Every icon in the Rules Reference has a glossary entry whose heading is the
    icon's name followed by the glyph itself -- `MENTAL RESOURCE ()`. So the
    legend does not have to be written down and kept true by hand; it is read
    out of the document, and any glyph that fails to resolve is an error rather
    than a silent `[icon]` in the output.
    """
    legend: Dict[str, str] = {}
    for index in range(GLOSSARY_FIRST_PAGE - 1, GLOSSARY_LAST_PAGE):
        for line in page_lines(pdf.pages[index], _is_furniture):
            if not line.heading:
                continue
            glyphs = [ch for span in line.spans if span.icon for ch in span.text]
            if not glyphs:
                continue
            title = "".join(span.text for span in line.spans if not span.icon)
            name = re.sub(r'\(\s*\)', '', title).strip()
            name = name.split(",")[0]
            name = re.sub(r'\bICON\b', '', name, flags=re.I)
            name = re.sub(r'\bRESOURCE\b', '', name, flags=re.I)
            name = slug(name) or slug(title)
            for glyph in glyphs:
                legend.setdefault(glyph, name)
    return legend


def alias_map(entries: Sequence[Entry]) -> Dict[str, str]:
    """Every name an entry can be cited by, mapped to its canonical slug.

    The document titles several entries as a list -- `BOOST, BOOST ICON`,
    `CONFUSE, CONFUSED`, `ALTER-EGO, ALTER-EGO FORM` -- and then cites them in
    `See also` by whichever half fits the sentence. Without this, "Boost"
    resolves to nothing while `boost-boost-icon.md` sits right there, and
    roughly a sixth of all cross-references dangle.
    """
    aliases: Dict[str, str] = {}
    for entry in entries:
        canonical = slug(entry.title)
        aliases[canonical] = canonical
    for entry in entries:
        canonical = slug(entry.title)
        for part in entry.title.split(","):
            key = slug(part)
            # An alias never displaces a real entry: `IDENTITY` is its own
            # entry as well as half of another title, and the entry wins.
            if key and key not in aliases:
                aliases[key] = canonical
    return aliases


def _sentence(text: str) -> str:
    """The first sentence, for the citation index's `fragment`."""
    plain = re.sub(r'\*+', '', text).strip()
    match = re.search(r'^(.+?[.!?])(?:\s|$)', plain)
    fragment = match.group(1) if match else plain
    return fragment.strip()


def _hash(text: str) -> str:
    normal = re.sub(r'\s+', ' ', re.sub(r'\*+', '', text)).strip()
    return "sha256:" + hashlib.sha256(normal.encode("utf-8")).hexdigest()


def build_index(entries: Sequence[Entry], legend: Dict[str, str]) -> Dict:
    """The citation index: one record per citable unit."""
    aliases = alias_map(entries)
    records: List[Dict] = []
    for entry in entries:
        key = slug(entry.title)
        if entry.redirect:
            records.append({
                "id": f"rr:{key}",
                "title": entry.title,
                "path": [entry.title],
                "page": entry.page,
                "redirect": entry.redirect,
            })
            continue

        records.append({
            "id": f"rr:{key}",
            "title": entry.title,
            "path": [entry.title],
            "page": entry.page,
            "fragment": _sentence(entry.definition),
            "hash": _hash(entry.definition),
            "see_also": [f"rr:{aliases[slug(name)]}" for name in entry.see_also
                         if slug(name) in aliases],
            "see_also_unresolved": [name for name in entry.see_also
                                    if slug(name) not in aliases],
        })
        for number, clause in enumerate(entry.clauses, start=1):
            records.append({
                "id": f"rr:{key}.{number}",
                "title": entry.title,
                "path": [entry.title, f"clause {number}"],
                "page": entry.page,
                "fragment": _sentence(clause.text),
                "hash": _hash(clause.text),
            })
            for sub, qualification in enumerate(clause.children, start=1):
                records.append({
                    "id": f"rr:{key}.{number}.{sub}",
                    "title": entry.title,
                    "path": [entry.title, f"clause {number}", f"qualification {sub}"],
                    "page": entry.page,
                    "fragment": _sentence(qualification.text),
                    "hash": _hash(qualification.text),
                })

    return {
        "document": "Marvel Champions: The Card Game -- Rules Reference",
        "version": RR_VERSION,
        "tier": "rr",
        "entry_count": len(entries),
        "record_count": len(records),
        "icons": legend,
        "entries": records,
    }


def _front_matter(fields: Dict) -> str:
    """Minimal YAML: scalars and flat lists only, so parsing it back is trivial."""
    lines = ["---"]
    for key, value in fields.items():
        if isinstance(value, list):
            if not value:
                lines.append(f"{key}: []")
            else:
                rendered = ", ".join(json.dumps(item) for item in value)
                lines.append(f"{key}: [{rendered}]")
        elif isinstance(value, int):
            lines.append(f"{key}: {value}")
        else:
            lines.append(f"{key}: {json.dumps(value)}")
    lines.append("---")
    return "\n".join(lines)


def render_markdown(entry: Entry, known: Dict[str, str]) -> str:
    """One entry as a linked markdown document.

    `known` maps slug to title for every entry in the corpus, so a `See also`
    pointing at something that does not exist is left as plain text rather than
    becoming a link to a missing file.
    """
    key = slug(entry.title)
    fields: Dict = {
        "id": f"rr:{key}",
        "title": entry.title,
        "document": "Rules Reference",
        "version": RR_VERSION,
        "page": entry.page,
    }
    if entry.redirect:
        fields["redirect"] = entry.redirect
    else:
        fields["hash"] = _hash(entry.definition)
    if entry.icons:
        fields["icons"] = entry.icons
    fields["see_also"] = [f"rr:{known[slug(name)]}" for name in entry.see_also
                          if slug(name) in known]

    out = [_front_matter(fields), "", f"# {entry.title}", ""]

    if entry.redirect:
        target = known.get(slug(entry.redirect))
        link = (f"[{entry.redirect}]({target}.md)" if target
                else entry.redirect)
        out += [f"See: {link}", ""]
        return "\n".join(out)

    if entry.definition:
        out += [entry.definition, ""]

    for number, clause in enumerate(entry.clauses, start=1):
        out.append(f'<a id="{key}-{number}"></a>')
        out.append(f"{number}. {clause.text}")
        for sub, qualification in enumerate(clause.children, start=1):
            out.append(f'    <a id="{key}-{number}-{sub}"></a>')
            out.append(f"    - {qualification.text}")
        out.append("")

    if entry.see_also:
        links = []
        for name in entry.see_also:
            target = known.get(slug(name))
            links.append(f"[{name}]({target}.md)" if target else name)
        out += ["**See also:** " + ", ".join(links), ""]

    return "\n".join(out)


def harvest(pdf_path: str) -> Dict[str, str]:
    """Parse the PDF. Returns the file tree to write, path -> contents."""
    import pdfplumber

    with pdfplumber.open(pdf_path) as pdf:
        legend = icon_legend(pdf)

        lines, pages = [], []
        for index in range(GLOSSARY_FIRST_PAGE - 1, GLOSSARY_LAST_PAGE):
            for line in page_lines(pdf.pages[index], _is_furniture):
                lines.append(line)
                pages.append(index + 1)

        entries = parse_entries(lines, pages, legend)

    unresolved = set()
    for entry in entries:
        for text in [entry.definition] + [c.text for c in entry.clauses]:
            unresolved |= {ch for ch in text if 0xF000 <= ord(ch) <= 0xF8FF}
    if unresolved:
        raise SystemExit(
            "unmapped icon glyphs, so the legend is incomplete: "
            + ", ".join(f"U+{ord(ch):04X}" for ch in sorted(unresolved)))

    seen: Dict[str, str] = {}
    for entry in entries:
        key = slug(entry.title)
        if key in seen:
            raise SystemExit(f"duplicate entry slug {key!r}: "
                             f"{seen[key]!r} and {entry.title!r}")
        seen[key] = entry.title

    aliases = alias_map(entries)

    tree = {
        "index.json": json.dumps(build_index(entries, legend),
                                 indent=2, ensure_ascii=False) + "\n",
        "icons.json": json.dumps(legend, indent=2, ensure_ascii=False) + "\n",
    }
    for entry in entries:
        tree[os.path.join("entries", f"{slug(entry.title)}.md")] = \
            render_markdown(entry, aliases)
    return tree


# Hand-written provenance, not generated. `--check` must not read it as drift.
NOT_GENERATED = ("UPSTREAM.md",)


def _read_tree(root: str) -> Dict[str, str]:
    existing: Dict[str, str] = {}
    for base, _, names in os.walk(root):
        for name in names:
            if name in NOT_GENERATED:
                continue
            if not (name.endswith(".json") or name.endswith(".md")):
                continue
            path = os.path.join(base, name)
            relative = os.path.relpath(path, root)
            with open(path, encoding="utf-8") as handle:
                existing[relative] = handle.read()
    return existing


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Harvest the Rules Reference into a citable rules index.")
    parser.add_argument("--pdf", default=DEFAULT_PDF)
    parser.add_argument("--out", default=OUT_DIR)
    parser.add_argument("--check", action="store_true",
                        help="compare against the committed snapshot; write nothing")
    args = parser.parse_args(argv)

    if not os.path.exists(args.pdf):
        print(f"Rules Reference not found: {args.pdf}", file=sys.stderr)
        print("The PDFs are not in this repository. Pass --pdf, or see "
              "datasets/rules-reference/UPSTREAM.md.", file=sys.stderr)
        return 2

    tree = harvest(args.pdf)

    if args.check:
        existing = _read_tree(args.out) if os.path.isdir(args.out) else {}
        added = sorted(set(tree) - set(existing))
        removed = sorted(set(existing) - set(tree))
        changed = sorted(k for k in set(tree) & set(existing)
                         if tree[k] != existing[k])
        if not (added or removed or changed):
            print(f"{args.out} is up to date "
                  f"({len(tree) - 2} entries, RR v{RR_VERSION})")
            return 0
        for name in added:
            print(f"  + {name}")
        for name in removed:
            print(f"  - {name}")
        for name in changed:
            print(f"  ~ {name}")
        print(f"\n{args.out} is stale: "
              f"{len(added)} added, {len(removed)} removed, {len(changed)} changed")
        print("Regenerate with: python -m tools.rules.harvest")
        return 1

    entries_dir = os.path.join(args.out, "entries")
    if os.path.isdir(entries_dir):
        for name in os.listdir(entries_dir):
            if name.endswith(".md"):
                os.remove(os.path.join(entries_dir, name))
    os.makedirs(entries_dir, exist_ok=True)

    for relative, contents in sorted(tree.items()):
        path = os.path.join(args.out, relative)
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(contents)

    print(f"wrote {len(tree) - 2} entries and "
          f"{json.loads(tree['index.json'])['record_count']} citable records "
          f"to {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
