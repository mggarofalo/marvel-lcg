"""Rules Reference lines in, structured rule entries out.

The Rules Reference glossary is regular enough to parse and irregular enough
that the regularity has to be checked rather than assumed. Every entry has the
same shape:

    HEADING
    an opening definition, one or more paragraphs
    ••  a top-level clause
          a qualification of that clause
    See also : Other Entry, Another Entry

## The two-tier citation grain

`docs/rules-provenance.md` asks for citations at the grain a spec actually
argues from, and that is not the entry. "Cost" is 30 clauses long and a spec
asserting overpayment behaviour is arguing from exactly one of them. So each
entry yields:

  * `rr:<slug>`      -- the entry, and its opening definition
  * `rr:<slug>.<n>`  -- the nth top-level clause
  * `rr:<slug>.<n>.<m>` -- the mth qualification of that clause

Ids are positional because the alternative -- deriving them from the text --
would change every id whenever wording changed, which is precisely when a
citation most needs to survive. A clause inserted mid-entry does renumber the
clauses after it; that is real, and it is why `tools.rules.diff` exists.

## Depth comes from markers, not from geometry

The obvious approach is to read indentation off the x coordinate. It does not
work: the left margin of the text block is not constant across pages, so a
sub-clause on page 12 can start further left than a top-level clause on page
30. The markers are reliable where the geometry is not -- a top-level clause
begins `••`, a qualification begins with two spaces (one space occurs
incidentally mid-paragraph and means nothing).
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Dict, List, Sequence

from tools.rules.geometry import Line, Span

# The doubled section banners ("OOVVEERRVVIIEEWW"). InDesign renders these
# titles twice with a small offset to fake a heavier weight, so the extracted
# text has every character duplicated. They are section markers, not entries.
DOUBLED = re.compile(r'^(?:(.)\1)+$')

BULLET = "••"
SUB_INDENT = "  "

SEE_ALSO = re.compile(r'^See\s+also\s*:\s*(.+)$', re.I)
SEE_ONLY = re.compile(r'^See\s*:\s*(.+)$', re.I)


@dataclass
class Clause:
    """One numbered unit of a rule: a bullet, or a qualification of one."""
    text: str
    children: List["Clause"] = field(default_factory=list)


def clean_title(title: str) -> str:
    """The heading without its icon tokens.

    An entry that names an icon prints the glyph after the words --
    `MENTAL RESOURCE ()`. The glyph is worth keeping, but in the `icons` field
    where it can be looked up, not inlined into every heading, link label and
    page title as `MENTAL RESOURCE ([mental])`.
    """
    text = re.sub(r'\(\s*(?:\[[a-z-]+\]\s*)+\)', '', title)
    text = re.sub(r'\[[a-z-]+\]', '', text)
    return re.sub(r'\s+', ' ', text).strip()


@dataclass
class Entry:
    """One Rules Reference glossary entry."""
    title: str
    page: int
    definition: str = ""
    clauses: List[Clause] = field(default_factory=list)
    see_also: List[str] = field(default_factory=list)
    redirect: str = ""          # set for "See: Other Entry" stub entries
    icons: List[str] = field(default_factory=list)


def is_banner(text: str) -> bool:
    """A doubled-render section title rather than an entry heading."""
    squashed = text.replace(" ", "")
    return bool(squashed) and bool(DOUBLED.match(squashed))


def _render(spans: Sequence[Span], icons: Dict[str, str]) -> str:
    """Spans to markdown, with icon glyphs resolved to named tokens."""
    out: List[str] = []
    for span in spans:
        text = span.text
        if span.icon:
            out.append("".join(f"[{icons.get(ch, 'icon')}]" for ch in text))
            continue
        if not text.strip():
            out.append(text)
            continue
        lead = text[:len(text) - len(text.lstrip())]
        trail = text[len(text.rstrip()):]
        body = text.strip()
        # Bullet glyphs are set in the bold face, which is typography rather
        # than emphasis. Left alone they render as `**••**`, and then the
        # structure test below -- which looks for a line *starting* with the
        # marker -- never fires and every entry parses as a single paragraph.
        # Two kinds of styling in this document are typography rather than
        # meaning, and both corrupt the output if taken at face value. Bullet
        # glyphs are set in the bold face -- left alone they render `**••**`,
        # and the structure test below, which looks for a line *starting* with
        # the marker, then never fires and every entry parses as one
        # paragraph. Brackets around an icon inherit the icon's italic, which
        # renders `*(*→*)*`.
        if body and not any(ch.isalnum() for ch in body):
            out.append(f"{lead}{body}{trail}")
            continue
        if span.bold:
            body = f"**{body}**"
        elif span.italic:
            body = f"*{body}*"
        out.append(f"{lead}{body}{trail}")
    return "".join(out)


def _join(parts: Sequence[str]) -> str:
    """Rejoin wrapped lines into one paragraph."""
    text = " ".join(part.strip() for part in parts if part.strip())
    text = re.sub(r'\s+([,.;:’”)])', r'\1', text)
    text = re.sub(r'\s{2,}', ' ', text)
    # Adjacent emphasis runs split across a line wrap, e.g. "**Forced** **Interrupt**".
    text = re.sub(r'\*\*\s+\*\*', ' ', text)
    text = re.sub(r'(?<!\*)\*\s+\*(?!\*)', ' ', text)
    return text.strip()


def parse_entries(lines: Sequence[Line], page_of: Sequence[int],
                  icons: Dict[str, str]) -> List[Entry]:
    """Segment a run of lines into entries.

    `page_of[i]` is the printed page `lines[i]` came from, so a citation can
    say where to look in the PDF.
    """
    entries: List[Entry] = []
    entry: Entry | None = None

    opening: List[str] = []
    clause: Clause | None = None
    child: Clause | None = None
    buffer: List[str] = []
    see_buffer: List[str] = []

    def flush() -> None:
        nonlocal buffer, clause, child
        if not buffer:
            return
        text = _join(buffer)
        buffer = []
        if not text:
            return
        if child is not None:
            child.text = _join([child.text, text]) if child.text else text
        elif clause is not None:
            clause.text = _join([clause.text, text]) if clause.text else text
        else:
            opening.append(text)

    def close() -> None:
        nonlocal entry, opening, clause, child
        flush()
        if entry is not None:
            entry.definition = _join(opening)
            if see_buffer:
                names = " ".join(part.strip() for part in see_buffer)
                entry.see_also = [name.strip() for name in names.split(",")
                                  if name.strip()]
            entries.append(entry)
        opening, clause, child = [], None, None
        see_buffer.clear()

    for index, line in enumerate(lines):
        raw = _render(line.spans, icons)
        stripped = raw.strip()
        if not stripped:
            continue
        # `See also` is set in bold, so it arrives as `**See also**: Ally, ...`.
        # Structure is matched against the de-emphasised text and content is
        # taken from it too -- a cross-reference list is a list of entry names,
        # and carrying the document's styling into them would mean every link
        # target had to be un-styled again before it could be resolved.
        plain = re.sub(r'\*+', '', stripped).strip()

        if line.heading:
            if is_banner(stripped):
                continue
            # A heading that wrapped the column, e.g. "PLAY RESTRICTIONS AND"
            # / "PERMISSIONS". Nothing separates the two lines but the column
            # width, so a second heading arriving before any body text is a
            # continuation of the first rather than a new entry.
            if (entry is not None and not opening and not entry.clauses
                    and not buffer and not entry.definition
                    and not entry.redirect and not see_buffer):
                entry.title = clean_title(f"{entry.title} {stripped}")
                continue
            close()
            entry = Entry(title=clean_title(stripped), page=page_of[index])
            entry.icons = [icons[ch] for span in line.spans if span.icon
                           for ch in span.text if ch in icons]
            continue

        if entry is None:
            continue

        match = SEE_ALSO.match(plain)
        if match:
            flush()
            see_buffer.clear()
            see_buffer.append(match.group(1))
            clause = child = None
            continue
        if SEE_ONLY.match(plain) and not entry.clauses and not opening:
            entry.redirect = SEE_ONLY.match(plain).group(1).strip()
            continue
        # A `See also` list is the last thing in an entry and routinely wraps.
        # It is accumulated whole and split on commas once, at `close()`,
        # rather than split per line: the column is narrow enough to break a
        # list *inside* a name, and splitting eagerly turned
        # "Initiating an Ability" into the two non-existent entries
        # "Initiating" and "Abilities".
        if see_buffer:
            see_buffer.append(plain)
            continue

        if plain.startswith(BULLET):
            flush()
            clause = Clause(text="")
            child = None
            entry.clauses.append(clause)
            buffer = [stripped.lstrip("*").lstrip()[len(BULLET):]]
            continue

        if raw.startswith(SUB_INDENT) and clause is not None:
            flush()
            child = Clause(text="")
            clause.children.append(child)
            buffer = [stripped]
            continue

        buffer.append(stripped)

    close()
    return entries
