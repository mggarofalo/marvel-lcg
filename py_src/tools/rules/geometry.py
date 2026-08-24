"""Turning a two-column InDesign PDF back into ordered, styled lines.

Split out from `harvest.py` because it is the part with no opinions about
Marvel Champions in it: give it a page, get back lines in reading order with
their fonts intact. Everything that knows what a *rule* is lives next door.

## Why font, not capitalisation

Entry headings in the Rules Reference are set in `ExoMVC-Bold` at 12pt and
nothing else in the document is. The first version of this harvester detected
headings by testing whether a line was all-capitals, which is the obvious
approach and quietly wrong in three ways: it clipped headings carrying an icon
glyph (the glyphs sit in a private-use range that a naive character class
excludes), it split `PLAY RESTRICTIONS AND PERMISSIONS` across the column wrap
into two entries, and it swept up the doubled section banners. Font matching has
none of those failure modes, and both methods finding exactly 266 headings is
how this one was checked.

## Why columns need explicit handling

Body pages are two columns with a gutter at x≈300 on a 612pt page. Sorting
characters by `(top, x0)` -- the obvious reading order -- interleaves the two
columns line by line and produces text that is locally fluent and globally
nonsense. Columns are separated first, then each is read top to bottom.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Callable, Dict, List, Sequence, Tuple

# Where to look for the gutter on the 612pt page this document uses, and the
# fallback if the search comes up empty.
#
# The gutter is found per page rather than fixed, because this document has
# two of them: recto and verso carry different margins, so the empty band sits
# at roughly 291-308 on one and 303-321 on the other. A single split at 300 is
# right for the first and *inside the text* of the second -- it clipped the
# final character off any left-column line that ran long, which is how
# "a hero does not exhaust" reached the corpus as "a hero does not exhaus".
# One dropped letter per affected line, in a dataset whose entire purpose is
# to be quotable.
GUTTER_SEARCH = (200, 400)
COLUMN_SPLIT = 300.0

# A real gutter is wider than the space between two words.
MIN_GUTTER_WIDTH = 6

# A heading is this font at this size, and nothing else in the document is.
HEADING_FONT = "ExoMVC-Bold"
HEADING_MIN_SIZE = 11.0
HEADING_ICON_MAX_SIZE = 12.5

# The icon font. Its characters live in a private-use range, so they survive
# text extraction as U+F5xx and would otherwise be mistaken for corruption.
ICON_FONT = "MarvelLCGIcons"

# The markers of the document's numbered procedures -- "1." "2." "a." "b." --
# which are set in their own face and nothing else uses it. That is what makes
# the procedures recoverable: the Rules Reference writes several of its most
# load-bearing rules as ordered steps ("When an enemy initiates an attack,
# follow these steps: ..."), and it cites them that way too -- "during step
# three of the villain phase" appears verbatim in three other entries. A
# procedure flattened into prose cannot be cited that way, and cannot answer
# "was a step added" at all, which is the question RR v1.8 poses (MARVEL-171).
MARKER_FONT = "Exo2-Black"

# Emphasis inside body text. Avenir-Black and Avenir-Heavy are both used for
# bold; the document does not distinguish them semantically.
BOLD_FONTS = ("Avenir-Black", "Avenir-Heavy")
ITALIC_FONTS = ("Avenir-BookOblique", "Avenir-HeavyOblique")

# The fonts the *rules text* is set in, at the sizes it is set at. Everything
# else on the page is dropped.
#
# This is an allowlist rather than a denylist of furniture, and the difference
# is not stylistic. Several pages carry a card illustration with callout labels
# -- "ATK" in FuturaBT-ExtraBlack, "ENCOUNTER GROUP" in Futura-CondensedMedium,
# card names in Exo2-Bold-SC700 -- and those labels are positioned freely, so
# one of them can share a baseline with a real entry heading. When that
# happened the two merged into a single line, the line began with the callout
# rather than the heading, and the entry was classified as body text and
# silently absorbed into the entry above it. `BOOST, BOOST ICON` disappeared
# from the corpus exactly this way, taking six cross-references with it.
#
# Size matters as much as face: the figures set Avenir at 13.4pt where the body
# uses 8.5pt, so matching on font name alone still lets callouts through.
BODY_FONTS = ("Avenir-Book", "Avenir-BookOblique", "Avenir-Black",
              "Avenir-Heavy", "Avenir-HeavyOblique", "MarvelLCGIcons",
              # The "1. 2. 3." / "a. b. c." markers of numbered procedures.
              "Exo2-Black")
BODY_MAX_SIZE = 9.5

# Page furniture, identified by its own fonts. Both are used for nothing else
# in the document, which makes this the reliable test -- the running header is
# letter-spaced ("Ru l e s  R e f e R e n c e") so matching it as text is
# fragile, and it straddles the gutter, so separating the columns cuts it in
# half and each half then fails a text match anyway. That is how fragments of
# it ended up inside `See also` lists as entries named "villain-eference".
#
# Deliberately *not* filtered: Exo2-Black, which sets the "1. 2. 3." markers of
# the document's numbered procedures. Those are content.
FURNITURE_FONTS = ("Exo2-Regular-SC850", "Exo2-ExtraBoldItalic")

# Two characters on the same line never differ in `top` by more than this, and
# a new line always differs by more. Derived from 8.5pt body text on ~11pt
# leading.
LINE_TOLERANCE = 3.0


@dataclass(frozen=True)
class Profile:
    """Which fonts play which role in one family of documents.

    The Rules Reference and the pack documents are set by the same publisher in
    different templates: the RR headings are `ExoMVC-Bold`, an insert's are
    `Exo2-ExtraBoldItalic`, and an insert marks its flavour text -- the
    S.H.I.E.L.D. briefings -- by setting whole pages in the oblique face. So
    the roles are constant across the corpus and the *fonts filling them* are
    not, which is what this exists to carry.
    """
    heading_fonts: Tuple[str, ...] = (HEADING_FONT,)
    heading_min_size: float = HEADING_MIN_SIZE
    body_fonts: Tuple[str, ...] = BODY_FONTS
    body_max_size: float = BODY_MAX_SIZE
    # A floor as well as a ceiling. The pack documents set their credits and
    # legal fine print in the same face as their rules, two or three points
    # smaller -- so size is what separates "this is how the scenario works"
    # from "Graphic Design: Chris Beck". The Rules Reference has no such text,
    # which is why the default admits everything.
    body_min_size: float = 0.0
    # What to do when no gutter is found. The Rules Reference is two columns on
    # every page, so a page that appears to have none is a detection failure
    # and the measured constant is the safer answer. The pack documents mix
    # one- and two-column pages freely, and forcing a split on a single-column
    # page cuts centred headings in half -- "COMPONENTS" arrived as "COM" and
    # "PONENTS", as two separate sections.
    fallback_split: float | None = COLUMN_SPLIT
    furniture_fonts: Tuple[str, ...] = FURNITURE_FONTS
    icon_font: str = ICON_FONT
    heading_icon_max_size: float = HEADING_ICON_MAX_SIZE
    marker_font: str = MARKER_FONT
    bold_fonts: Tuple[str, ...] = BOLD_FONTS
    italic_fonts: Tuple[str, ...] = ITALIC_FONTS


RULES_REFERENCE = Profile()


@dataclass
class Span:
    """A run of characters sharing one style."""
    text: str
    bold: bool = False
    italic: bool = False
    icon: bool = False


@dataclass
class Line:
    """One rendered line, with enough geometry to infer bullet depth."""
    spans: List[Span] = field(default_factory=list)
    x0: float = 0.0
    top: float = 0.0
    size: float = 0.0
    heading: bool = False
    # The leading numbered-procedure marker, if this line starts a step:
    # "1" for `1.`, "a" for `a.`. Empty when the line is ordinary text.
    marker: str = ""

    @property
    def text(self) -> str:
        return "".join(span.text for span in self.spans)


def _font(char: Dict) -> str:
    """The font name without the subset prefix InDesign stamps on it."""
    return char.get("fontname", "").split("+")[-1]


def _is_heading(char: Dict, profile: Profile = RULES_REFERENCE) -> bool:
    font, size = _font(char), char.get("size", 0)
    return (any(h in font for h in profile.heading_fonts)
            and size >= profile.heading_min_size)


def _is_content(char: Dict, profile: Profile = RULES_REFERENCE) -> bool:
    """Is this character part of the rules text, rather than of a figure?"""
    font, size = _font(char), char.get("size", 0.0)
    if size >= profile.heading_min_size:
        # Heading-sized: the heading face at any size, and icons only at the
        # size headings set them. The card figures print the same glyphs much
        # larger (13.4pt and 16.75pt against a heading's 12pt), and without the
        # ceiling those decorative copies are read as part of the rule and
        # prepended to its text.
        if font == profile.icon_font:
            return size <= profile.heading_icon_max_size
        return any(h in font for h in profile.heading_fonts)
    return (any(body in font for body in profile.body_fonts)
            and profile.body_min_size <= size <= profile.body_max_size)


def _leading_marker(row: Sequence[Dict], profile: Profile = RULES_REFERENCE) -> str:
    """The step number or letter this line opens with, if any."""
    text = ""
    for char in row:
        if _font(char) != profile.marker_font:
            break
        text += char["text"]
    match = re.match(r'\s*([0-9]+|[a-z])\.\s*$', text)
    return match.group(1) if match else ""


def _style(char: Dict, profile: Profile = RULES_REFERENCE) -> tuple:
    font = _font(char)
    return (
        any(b in font for b in profile.bold_fonts),
        any(i in font for i in profile.italic_fonts),
        font == profile.icon_font,
    )


def _to_lines(chars: Sequence[Dict],
              profile: Profile = RULES_REFERENCE) -> List[Line]:
    """Group characters into lines, merging runs that share a style.

    Grouping is by vertical position and ordering is by horizontal position,
    and those are two passes rather than one sort. A single sort on
    `(top, x0)` looks equivalent and is not: bullet glyphs and bold runs sit a
    fraction of a point off the baseline of the text they belong to, so they
    land in the right *line* but sort after everything on it. That put every
    bullet marker at the end of its own bullet and turned `"Forced Interrupt:
    When this character would scheme"` into `": When this character would
    scheme Forced Interrupt"`. Group first, then order within the group.
    """
    if not chars:
        return []

    rows: List[List[Dict]] = []
    current: List[Dict] = []
    reference: float | None = None
    for char in sorted(chars, key=lambda c: c["top"]):
        if reference is None or abs(char["top"] - reference) <= LINE_TOLERANCE:
            if reference is None:
                reference = char["top"]
            current.append(char)
        else:
            rows.append(current)
            current, reference = [char], char["top"]
    if current:
        rows.append(current)

    lines: List[Line] = []
    for row in rows:
        row.sort(key=lambda c: c["x0"])
        first = row[0]
        # Heading-ness is decided by the first character that is not a space.
        # Several headings are typeset with a leading space that belongs to
        # the *body* font, and testing the literal first character therefore
        # classified them as body text -- which silently merged each one into
        # the entry above it.
        marker = next((c for c in row if c["text"].strip()), first)
        line = Line(x0=first["x0"], top=first["top"],
                    size=marker.get("size", 0.0),
                    heading=_is_heading(marker, profile),
                    marker=_leading_marker(row, profile))
        for char in row:
            bold, italic, icon = _style(char, profile)
            if (line.spans and line.spans[-1].bold == bold
                    and line.spans[-1].italic == italic
                    and line.spans[-1].icon == icon):
                line.spans[-1].text += char["text"]
            else:
                line.spans.append(Span(char["text"], bold, italic, icon))
        lines.append(line)

    return lines


def column_split(chars: Sequence[Dict],
                 profile: Profile | None = None) -> float | None:
    """The x coordinate separating the two columns, found by looking.

    The widest vertical band no character touches, within the range a gutter
    could plausibly occupy. Returns the fallback when the page is a single
    column, or when nothing clean enough to be a gutter turns up.
    """
    fallback = COLUMN_SPLIT if profile is None else profile.fallback_split
    if not chars:
        return fallback

    low, high = GUTTER_SEARCH
    covered = bytearray(high - low)
    for char in chars:
        start = max(low, int(char["x0"]))
        stop = min(high, int(char["x1"]) + 1)
        for x in range(start, stop):
            covered[x - low] = 1

    best_width, best_start, run = 0, None, 0
    for offset, filled in enumerate(covered):
        if filled:
            run = 0
            continue
        run += 1
        if run > best_width:
            best_width, best_start = run, offset - run + 1

    if best_start is None or best_width < MIN_GUTTER_WIDTH:
        return fallback
    return low + best_start + best_width / 2.0


def straddling(chars: Sequence[Dict], split: float) -> List[Dict]:
    """Characters the split runs through, which means it is not a gutter.

    A column boundary that cuts a glyph in half is not a boundary. This is the
    cheap invariant that would have caught the fixed 300pt split immediately:
    on half the pages it sat inside the left column's text, so the last
    character of a long line was filed under the *other* column and reappeared
    somewhere else entirely. Nothing was lost, so no count could notice; the
    text was simply wrong.
    """
    return [c for c in chars if c["x0"] < split < c["x1"]]


def page_lines(page, skip: Callable[[str], bool] | None = None,
               profile: Profile = RULES_REFERENCE) -> List[Line]:
    """Every line on `page`, left column first, then right.

    A heading wide enough to cross the gutter is assigned to the left column,
    which is where it starts and where it reads.
    """
    chars = [c for c in page.chars
             if _is_content(c, profile)
             and not any(f in _font(c) for f in profile.furniture_fonts)]
    split = column_split(chars, profile)
    if split is None:
        lines = _to_lines(chars, profile)
    else:
        left = [c for c in chars if c["x0"] < split]
        right = [c for c in chars if c["x0"] >= split]
        lines = _to_lines(left, profile) + _to_lines(right, profile)
    if skip is not None:
        lines = [line for line in lines if not skip(line.text.strip())]
    return [line for line in lines if line.text.strip()]
