"""Behavioral spec harness for the Python reference engine.

A card behavior is expressed as Given / When / Then and run against the running
engine:

- **Given** builds a board state out of `game.puzzle.puzzle.RunPuzzle` commands
- **When** selects an effect through the headless bot device
- **Then** asserts over readable game state -- health, threat, zone, counters,
  statuses -- not over the replay CRC

See `docs/spec-harness.md` for the authoring vocabulary.
"""
