"""Determinism verification harness for the Python engine.

See `docs/determinism-audit.md` for what these tools check and why.

Everything in this package is additive tooling. Nothing here imports into the
engine's own code paths, and nothing here is meant to ship with the game.
"""
