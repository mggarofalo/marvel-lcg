"""Tooling for the v2 state digest.

The digest is a wire format -- `docs/state-digest-v2.md` -- so it gets the same
treatment the RNG contract got: a checked-in fixture the C# port is accepted
against, and a `--check` mode that fails when the Python side has moved without
the fixture moving with it.
"""
