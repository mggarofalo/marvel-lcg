"""Printed-card-text handling shared by both sources.

Card text is stored as a fragment of HTML: `<b>` for the ability keyword,
`<i>` for reminder text, `<hr />` between the two halves of a double-sided
card. Resource icons and card references are not HTML -- `[mental]` and
`[[Black Panther]]` are printed symbols, and they stay.
"""

from __future__ import annotations

import html
import re

# `<hr />` separates printed faces, so it becomes a line break rather than
# vanishing. Every other tag is formatting.
_HR = re.compile(r"<hr\s*/?>")
_TAG = re.compile(r"<[^>]*>")
_HORIZONTAL_SPACE = re.compile(r"[^\S\n]+")

# U+FFFD. Present in `py_src/data/cards.json` where a character was lost in an
# encoding round-trip; never present in the MarvelSDB snapshot.
REPLACEMENT_CHAR = "�"


def ToPlainText(text: str) -> str:
    """Strip HTML formatting from printed card text, keeping the words.

    HTML entities are unescaped, which matters because `data/cards.json` writes
    the same arrow as both `&#8594;` and a literal U+2192 -- comparing raw text
    would call those two cards different when they are not.
    """
    text = _HR.sub("\n", text or "")
    text = _TAG.sub("", text)
    text = html.unescape(text)
    lines = [_HORIZONTAL_SPACE.sub(" ", line).strip() for line in text.split("\n")]
    return "\n".join(lines).strip()


def IsCorrupt(text: str) -> bool:
    return REPLACEMENT_CHAR in (text or "")
