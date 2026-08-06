"""The environment every determinism run must be executed under.

Keep this file as the single definition of the pinned environment. The corpus
generator (MARVEL-5), the test runner, and CI should all import `build_env()`
rather than setting variables ad hoc, so that a change to the pin is a change
in one place.

Why each pin exists is documented in `docs/determinism-audit.md`.
"""

from __future__ import annotations

import os
from typing import Dict, Mapping


# Iteration order of any `set` of strings is derived from the per-process hash
# seed. Pinning it to 0 disables hash randomization entirely.
PYTHON_HASH_SEED = "0"

# Bytecode caches are irrelevant to determinism but writing them makes runs
# differ in wall time and in filesystem state, which muddies bisecting.
PYTHON_DONT_WRITE_BYTECODE = "1"

# Unbuffered stdout so a crashed run still yields the trace produced so far.
PYTHON_UNBUFFERED = "1"

# Force a stable text encoding. The engine logs card names containing symbols
# such as U+26A0; on a cp1252 console those raise and change control flow
# inside the logger.
PYTHON_IO_ENCODING = "utf-8"


def build_env(base: Mapping[str, str] | None = None) -> Dict[str, str]:
    """Return a copy of `base` (default: os.environ) with the pins applied."""
    env = dict(os.environ if base is None else base)
    env["PYTHONHASHSEED"] = PYTHON_HASH_SEED
    env["PYTHONDONTWRITEBYTECODE"] = PYTHON_DONT_WRITE_BYTECODE
    env["PYTHONUNBUFFERED"] = PYTHON_UNBUFFERED
    env["PYTHONIOENCODING"] = PYTHON_IO_ENCODING
    return env


def is_pinned() -> bool:
    """True when the current process is already running under the pins.

    `PYTHONHASHSEED` cannot be applied from inside a running interpreter, so a
    probe that depends on it has to re-exec. Callers use this to decide.
    """
    return os.environ.get("PYTHONHASHSEED") == PYTHON_HASH_SEED
