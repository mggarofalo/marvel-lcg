"""Tooling around the replay verification command.

The command itself is engine code -- `game/test/verify.py`, reached with
`python main.py -verify_replays`. This package holds what a single verification
run cannot check about itself: that the gate rejects a corpus it should reject.
Same split as the state digest and card coverage, where the contract lives in
the engine and the command line is a thin shell over it.
"""
