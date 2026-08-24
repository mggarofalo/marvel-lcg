"""Harvest the pack documents into the `pack:` tier of the rules corpus.

    python -m tools.rules.packs                 # write the snapshot
    python -m tools.rules.packs --check         # is the committed snapshot current?
    python -m tools.rules.packs --list          # what would be read, and how classified

Run from `py_src/`. Needs `pdfplumber` and the local PDF library; like
`tools.rules.harvest` this is a vendored harvest, not a CI gate. See
`datasets/rules-packs/UPSTREAM.md`.

## The second tier

MARVEL-154 defines two id tiers. `tools.rules.harvest` builds the first, the
Rules Reference. This builds the second:

    rr:<entry>[.<clause>][.step.<n>]   the Rules Reference -- the authority
    pack:<code>:<section>              per-pack rules

They are separate because the documents are separate kinds of thing. The Rules
Reference is one alphabetical glossary with a rigid shape. The pack documents
are 61 heterogeneous booklets -- an expansion rulebook, a scenario insert and a
hero rulesheet share a publisher's template and nothing else -- and they are
where new keywords and scenario-specific rules actually arrive.

## Finding the rules inside a marketing document

Most of a hero rulesheet is not rules. It is cover copy, a designer credit
list, and a paragraph of prose about Steve Rogers. Three signals separate the
rules from the rest, and all three are typographic rather than semantic,
because the alternative is guessing from wording:

  * **Size.** Credits and legal fine print are set in the body face two to
    three points smaller than the rules. `Profile.body_min_size` drops them.
  * **Slant.** Flavour is set oblique -- the S.H.I.E.L.D. briefings that open
    each scenario are whole pages of it. Rules are roman.
  * **Section.** What remains is grouped under headings, and a small denylist
    of heading names removes the sections that are reliably not rules.

The denylist is short and explicit rather than clever. An allowlist cannot
work: 531 distinct headings across the corpus, most of them scenario names,
keyword names and villain names that no list could anticipate. Excluding what
is known not to be rules keeps the unknown, which is the safer default for a
corpus whose purpose is to be complete.

## What this does not do

It does not author `references`. The rules corpus is a one-way graph -- an
exception names the rule it overrides, and a base rule names nothing (see
`docs/rules-provenance.md`) -- and which `rr:` rule a given pack section
modifies is a judgement, not something a parser can read off the page. The
field is emitted empty and filled in by hand. Inferring it from a title match
would produce exactly the kind of plausible, wrong relationship this corpus
exists to eliminate.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
from dataclasses import dataclass, field
from typing import Dict, List, Sequence

from tools.rules.geometry import Profile, page_lines
from tools.rules.parse import undouble

LIBRARY = os.path.expanduser("~/Documents/Marvel Champions LCG")
OUT_DIR = os.path.join("..", "datasets", "rules-packs")

# The pack template. Different faces from the Rules Reference, same roles --
# see `Profile`. `fallback_split=None` because these documents mix one- and
# two-column pages, and forcing a split on a single-column page cuts a centred
# heading in half.
PACK_PROFILE = Profile(
    heading_fonts=("Exo2-ExtraBoldItalic", "Exo2-Bold", "ExoMVC-Bold-SC700"),
    heading_min_size=10.0,
    body_fonts=("Avenir-Book", "Avenir-Black", "Avenir-Heavy",
                "Avenir-BookOblique", "Avenir-Oblique", "Avenir-HeavyOblique",
                "MarvelLCGIcons"),
    body_min_size=7.5,
    body_max_size=9.5,
    bold_fonts=("Avenir-Black", "Avenir-Heavy"),
    italic_fonts=("Avenir-BookOblique", "Avenir-Oblique", "Avenir-HeavyOblique"),
    furniture_fonts=(),
    fallback_split=None,
)

# Headings whose sections are reliably not rules. Measured against the corpus
# rather than imagined: each of these appears in at least three documents and
# carries cover copy, credits, narrative or advice.
NOT_RULES = {
    "CREDITS", "PLAYTESTERS", "MARVEL", "GAME",
    "HERO PACK", "SCENARIO PACK", "EXPANSION SYMBOL", "SET SYMBOL",
    "S.H.I.E.L.D. BRIEFING", "THE STORY SO FAR", "STRATEGY TIPS",
    "STRATEGY TIP", "COMPONENTS", "COMPONENT LIST",
}

# Campaign logs are play aids, not rules -- MARVEL-154 puts them out of scope.
OUT_OF_SCOPE = ("campaign_log", "campaignlog", "campaign-log")

# Order matters: the first match wins, and the patterns overlap. A scenario
# insert is named `..._rules_insert.pdf`, which contains `_rules_` -- so
# testing for a rulebook first classifies all 19 inserts as rulebooks.
KIND_PATTERNS = (
    ("learn-to-play", ("learn_to_play", "learntoplay")),
    ("insert", ("rules_insert", "rulesinsert", "rules_website")),
    ("rulesheet", ("rulesheet",)),
    ("rulebook", ("rulebook", "_rules_", "_rules-")),
)


@dataclass
class Rule:
    """A named rule inside a section: one bold sub-heading and its prose."""
    heading: str
    paragraphs: List[str] = field(default_factory=list)

    @property
    def text(self) -> str:
        return "\n\n".join(self.paragraphs)


@dataclass
class Section:
    heading: str
    page: int
    paragraphs: List[str] = field(default_factory=list)
    # The named rules under this heading. A pack's "NEW RULES" section is a
    # list of them -- "When the Villain Changes Form", "When a Villain Stage is
    # Defeated" -- each a self-contained rule that an agent would look up on
    # its own, and each set in the bold face. Run together into one blob they
    # are unusable: the reader has to find the sentence that applies inside
    # four paragraphs about a different situation.
    rules: List[Rule] = field(default_factory=list)

    @property
    def text(self) -> str:
        parts = list(self.paragraphs)
        for rule in self.rules:
            parts.append(rule.heading)
            parts.extend(rule.paragraphs)
        return "\n\n".join(parts)


@dataclass
class Document:
    path: str
    code: str
    kind: str
    title: str
    sections: List[Section] = field(default_factory=list)


def slug(text: str) -> str:
    text = undouble(text).lower()
    text = re.sub(r'\[[a-z-]+\]', ' ', text)
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")


def classify(filename: str) -> "tuple[str, str] | None":
    """`(pack code, kind)` for a filename, or None if out of scope."""
    low = filename.lower()
    if any(marker in low for marker in OUT_OF_SCOPE):
        return None
    if "rulesreference" in low:
        return None
    match = re.match(r'(mc\d+|mvc\d+)', low)
    code = match.group(1) if match else os.path.splitext(low)[0][:12]
    for kind, markers in KIND_PATTERNS:
        if any(marker in low for marker in markers):
            return code, kind
    return code, "other"


def _clean(text: str) -> str:
    text = undouble(re.sub(r'\s+', ' ', text).strip())
    return text


def read_document(path: str) -> Document:
    import pdfplumber

    filename = os.path.basename(path)
    classified = classify(filename)
    assert classified is not None
    code, kind = classified

    document = Document(path=filename, code=code, kind=kind, title="")
    section: Section | None = None
    rule: Rule | None = None
    buffer: List[str] = []
    heading_buffer: List[str] = []

    def flush() -> None:
        nonlocal buffer
        if section is not None and buffer:
            paragraph = _clean(" ".join(buffer))
            if paragraph:
                (rule.paragraphs if rule is not None
                 else section.paragraphs).append(paragraph)
        buffer = []

    def close_heading() -> None:
        """Turn accumulated bold lines into the next rule's heading."""
        nonlocal rule, heading_buffer
        if not heading_buffer:
            return
        heading = _clean(" ".join(heading_buffer))
        heading_buffer = []
        if section is None or not heading:
            return
        rule = Rule(heading=heading)
        section.rules.append(rule)

    with pdfplumber.open(path) as pdf:
        for index, page in enumerate(pdf.pages):
            for line in page_lines(page, None, PACK_PROFILE):
                text = line.text.strip()
                if not text:
                    continue
                if line.heading:
                    heading = _clean(text)
                    # Folios and rules set in the heading face.
                    if not heading or re.fullmatch(r'[\d\W]+', heading):
                        continue
                    flush()
                    heading_buffer = []
                    rule = None
                    section = Section(heading=heading, page=index + 1)
                    document.sections.append(section)
                    if not document.title:
                        document.title = heading
                    continue
                if section is None:
                    continue
                # Flavour is set oblique; rules are roman. A line that is
                # entirely italic is narrative, and dropping it here keeps the
                # briefings out without needing to recognise prose.
                if line.spans and all(span.italic or not span.text.strip()
                                      for span in line.spans):
                    continue
                # A wholly-bold line is a rule heading, and it can wrap, so
                # consecutive bold lines are one heading rather than two rules.
                if line.spans and all(span.bold or not span.text.strip()
                                      for span in line.spans):
                    if not heading_buffer:
                        flush()
                    heading_buffer.append(text)
                    continue
                close_heading()
                buffer.append(text)
        close_heading()
        flush()

    document.sections = [s for s in document.sections
                         if (s.paragraphs or s.rules)
                         and s.heading.upper() not in NOT_RULES]
    return document


def _hash(text: str) -> str:
    return "sha256:" + hashlib.sha256(
        re.sub(r'\s+', ' ', text).strip().encode("utf-8")).hexdigest()


def _sentence(text: str) -> str:
    plain = re.sub(r'\*+', '', text).strip()
    match = re.search(r'^(.+?[.!?])(?:\s|$)', plain)
    return (match.group(1) if match else plain).strip()


def _front_matter(fields: Dict) -> str:
    lines = ["---"]
    for key, value in fields.items():
        if isinstance(value, list):
            rendered = ", ".join(json.dumps(v) for v in value)
            lines.append(f"{key}: [{rendered}]" if value else f"{key}: []")
        elif isinstance(value, int):
            lines.append(f"{key}: {value}")
        else:
            lines.append(f"{key}: {json.dumps(value)}")
    lines.append("---")
    return "\n".join(lines)


def build(documents: Sequence[Document]) -> Dict[str, str]:
    records: List[Dict] = []
    tree: Dict[str, str] = {}
    seen: Dict[str, str] = {}

    for document in documents:
        for section in document.sections:
            key = slug(section.heading)
            identifier = f"pack:{document.code}:{key}"
            if identifier in seen:
                suffix = 2
                while f"{identifier}-{suffix}" in seen:
                    suffix += 1
                identifier = f"{identifier}-{suffix}"
                key = f"{key}-{suffix}"
            seen[identifier] = section.heading

            records.append({
                "id": identifier,
                "title": section.heading,
                "pack": document.code,
                "kind": document.kind,
                "source": document.path,
                "page": section.page,
                "fragment": _sentence(
                    section.paragraphs[0] if section.paragraphs
                    else section.rules[0].heading),
                "hash": _hash(section.text),
                "rules": len(section.rules),
                # One-way, and authored rather than parsed. See the module
                # docstring and docs/rules-provenance.md.
                "references": [],
            })

            for rule in section.rules:
                records.append({
                    "id": f"{identifier}.{slug(rule.heading)}",
                    "title": rule.heading,
                    "pack": document.code,
                    "kind": document.kind,
                    "source": document.path,
                    "page": section.page,
                    "fragment": _sentence(rule.text),
                    "hash": _hash(rule.text),
                    "references": [],
                })

            fields = {
                "id": identifier,
                "title": section.heading,
                "pack": document.code,
                "kind": document.kind,
                "source": document.path,
                "page": section.page,
                "hash": _hash(section.text),
                "rules": [slug(r.heading) for r in section.rules],
                "references": [],
            }
            body = [_front_matter(fields), "", f"# {section.heading}", ""]
            body += [paragraph + "\n" for paragraph in section.paragraphs]
            for rule in section.rules:
                anchor = slug(rule.heading)
                body.append(f'<a id="{anchor}"></a>')
                body.append(f"## {rule.heading}")
                body.append("")
                body += [paragraph + "\n" for paragraph in rule.paragraphs]
            tree[os.path.join(document.code, f"{key}.md")] = "\n".join(body)

    index = {
        "tier": "pack",
        "documents": len(documents),
        "record_count": len(records),
        "packs": sorted({d.code for d in documents}),
        "entries": records,
    }
    tree["index.json"] = json.dumps(index, indent=2, ensure_ascii=False) + "\n"
    return tree


def sources(library: str) -> List[str]:
    if not os.path.isdir(library):
        return []
    out = []
    for name in sorted(os.listdir(library)):
        if not name.lower().endswith(".pdf"):
            continue
        if classify(name) is None:
            continue
        out.append(os.path.join(library, name))
    return out


def _read_tree(root: str) -> Dict[str, str]:
    existing: Dict[str, str] = {}
    for base, _, names in os.walk(root):
        for name in names:
            if name == "UPSTREAM.md":
                continue
            if not (name.endswith(".json") or name.endswith(".md")):
                continue
            path = os.path.join(base, name)
            with open(path, encoding="utf-8") as handle:
                existing[os.path.relpath(path, root)] = handle.read()
    return existing


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Harvest the pack documents into the `pack:` rules tier.")
    parser.add_argument("--library", default=LIBRARY)
    parser.add_argument("--out", default=OUT_DIR)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--list", action="store_true")
    args = parser.parse_args(argv)

    paths = sources(args.library)
    if not paths:
        print(f"no pack documents under {args.library}", file=sys.stderr)
        print("The PDFs are not in this repository. Pass --library, or see "
              "datasets/rules-packs/UPSTREAM.md.", file=sys.stderr)
        return 2

    if args.list:
        for path in paths:
            code, kind = classify(os.path.basename(path))
            print(f"  {code:8s} {kind:14s} {os.path.basename(path)}")
        print(f"\n{len(paths)} document(s)")
        return 0

    documents = [read_document(path) for path in paths]
    tree = build(documents)

    if args.check:
        existing = _read_tree(args.out) if os.path.isdir(args.out) else {}
        added = sorted(set(tree) - set(existing))
        removed = sorted(set(existing) - set(tree))
        changed = sorted(k for k in set(tree) & set(existing)
                         if tree[k] != existing[k])
        if not (added or removed or changed):
            print(f"{args.out} is up to date "
                  f"({len(documents)} documents, {len(tree) - 1} sections)")
            return 0
        for name in added:
            print(f"  + {name}")
        for name in removed:
            print(f"  - {name}")
        for name in changed:
            print(f"  ~ {name}")
        print(f"\n{args.out} is stale: {len(added)} added, "
              f"{len(removed)} removed, {len(changed)} changed")
        return 1

    if os.path.isdir(args.out):
        for base, _, names in os.walk(args.out):
            for name in names:
                if name.endswith(".md") and name != "UPSTREAM.md":
                    os.remove(os.path.join(base, name))
    for relative in tree:
        os.makedirs(os.path.join(args.out, os.path.dirname(relative)) or args.out,
                    exist_ok=True)
    for relative, contents in sorted(tree.items()):
        with open(os.path.join(args.out, relative), "w",
                  encoding="utf-8", newline="\n") as handle:
            handle.write(contents)

    print(f"wrote {len(tree) - 1} sections from {len(documents)} documents "
          f"to {args.out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
