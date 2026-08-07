"""Card-text extraction tooling (MARVEL-19).

Builds the spec-authoring dataset in `datasets/cards/` by joining three sources:

- `datasets/marvelsdb/` -- the vendored MarvelSDB snapshot. **Authoritative for
  printed card text**, which is the whole point: specs authored from the words
  the designers wrote, not from an implementation nobody fully understands.
- `py_src/data/cards.json` -- what the Python engine believes each card says.
  Kept so divergence from the printed text is visible rather than assumed away.
- `py_src/cards/pack/**` -- the 3,457 card scripts, so every card links to the
  code that implements it today.

Everything here is stdlib-only and imports nothing from the engine. That is
deliberate: `cards/paper.py` and `cards/database.py` cannot be imported outside
a full engine bootstrap (`game.object` has a circular import), and the dataset
should stay generatable by anyone with a bare Python 3.13 -- including the C#
side, which consumes the output but has no reason to install numpy.

The cost is that the engine's load rules are mirrored rather than reused. Every
mirror names the engine code it copies, and `unit_test/test_card_dataset.py`
cross-checks the one rule that is easy to get subtly wrong (`CleanName`) against
the real implementation whenever the engine venv happens to be importable.

Run from `py_src/`:

    python -m tools.cards.extract
    python -m tools.cards.extract --check
"""
