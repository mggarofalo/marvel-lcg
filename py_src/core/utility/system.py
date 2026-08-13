import os
import sys

class System:

    # `title` and `pause` are cmd.exe builtins. Elsewhere the shell has no such
    # commands, so every engine boot wrote `sh: 1: title: not found` to stderr
    # -- harmless to the game, but the determinism and replay probes capture a
    # child process's stderr and report it when a check fails, so it turned
    # every Linux probe failure into a misleading one. See MARVEL-72.
    IS_WINDOWS = sys.platform == 'win32'

    @staticmethod
    def Run(cmd: str):
        os.system(cmd)

    @staticmethod
    def SetTitle(title: str):
        if not System.IS_WINDOWS:
            return
        System.Run(f'title {title}')

    @staticmethod
    def Pause():
        if not System.IS_WINDOWS:
            return
        System.Run('pause')

    @staticmethod
    def Sleep(secs: float):
        from time import sleep
        return sleep(secs)

    @staticmethod
    def UseUtf8Streams() -> None:
        """Make this process's text output UTF-8, whatever the locale says.

        The engine logs card names and board state containing symbols -- U+26A0
        for threat, U+1F0E2 for a ready ally, and the rest of
        `game/render/symbol.py`. None of them exist in cp1252, which is the
        encoding CPython picks for `sys.stdout` on Windows whenever stdout is
        not a console: a redirect, a pipe, a CI log. Writing one then raises
        `UnicodeEncodeError: 'charmap' codec can't encode character`, and the
        whole line is lost -- which is how MARVEL-36 was found, in a corpus run
        whose output was going to a file.

        Configuring the stream is the whole fix. It replaces the per-print
        `try/except` the logger used to carry: that could only report the line
        it had just failed to print, so a symbol turned a log line into an error
        message about a log line. There is nothing left for a fallback to do --
        UTF-8 encodes every string Python can hold.

        Called at import (see the bottom of this module) rather than from an
        entry point, because there are several -- `main.py`, `python -m
        unittest`, and each `tools/` command -- and every one of them reaches
        this module through `from core import *` before it can print anything.
        One call site that cannot be missed beats five that can.

        `errors` is deliberately left at `strict`, matching
        `PYTHONIOENCODING=utf-8` -- the pin `tools/determinism/pinned_env.py`
        applies to every determinism subprocess and `.github/workflows/ci.yml`
        to the whole matrix. Runs under the pin and runs without it should take
        the same code path, not two that differ in how they mangle output.

        Streams that cannot be reconfigured are left alone. `sys.stdout` is
        `None` under `pythonw.exe`, and a test or tool that has swapped in a
        `StringIO` has already chosen its own encoding by choosing an in-memory
        stream.
        """
        for stream in (sys.stdout, sys.stderr):
            reconfigure = getattr(stream, "reconfigure", None)
            if reconfigure == None:
                continue
            reconfigure(encoding="utf-8")


System.UseUtf8Streams()

