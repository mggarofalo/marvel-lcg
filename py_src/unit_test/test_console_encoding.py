"""A log line carrying a game symbol survives the encoding the OS picked.

`game/render/symbol.py` puts U+26A0 (threat), U+2661 (health) and a run of
playing-card characters above U+1F0A0 into ordinary log lines. None of them
exist in cp1252, which is what CPython chooses for `sys.stdout` on Windows when
stdout is not a console -- a redirect, a pipe, a CI log. Writing one raised
`UnicodeEncodeError: 'charmap' codec can't encode character`, the logger's
`try/except` caught it, and the line was replaced by `Error printing log: ...`.
Only readability, but MARVEL-36 was found in a corpus run of thousands of
unattended games, where a partially swallowed log is what triage has to work
from.

The fix is `System.UseUtf8Streams`, called when `core` is imported: the
encoding is a property of the process, settled once, rather than something the
print path guesses at per line. These tests pin both halves -- that an
unconfigured cp1252 stream really does destroy the line, and that a configured
one carries it through whole.

They run on a synthetic cp1252 stream rather than on the real one, deliberately.
The failure only reproduces on Windows, and even there CI pins
`PYTHONIOENCODING=utf-8` for the whole matrix, so a test that read the process's
own stdout would pass on every platform this repository can run and prove
nothing. A `TextIOWrapper` opened in cp1252 behaves identically on all three.

See MARVEL-36.
"""

import io
import sys
import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from core import System
from engine.log import Log
from game.render.symbol import Symbol


# One symbol per family that cp1252 cannot represent: the threat marker the
# issue was reported against, and an astral-plane card glyph, which is the case
# a naive "widen to latin-1" fix would still lose.
THREAT = "⚠"
CARD = "\U0001f0e2"


def Cp1252Stream():
    """A text stream encoded the way a redirected Windows stdout would be."""
    raw = io.BytesIO()
    # `newline=""` keeps the bytes comparable across platforms: the default
    # translates "\n" to os.linesep on write.
    return io.TextIOWrapper(raw, encoding="cp1252", newline=""), raw


class TestTheStreamIsTheProblem(unittest.TestCase):
    """Everything below is only meaningful because of these two."""

    def test_a_symbol_cannot_be_written_to_a_cp1252_stream(self):
        stream, _ = Cp1252Stream()

        with self.assertRaises(UnicodeEncodeError):
            stream.write(THREAT)
            stream.flush()

    def test_the_symbols_are_the_ones_the_engine_actually_logs(self):
        # Pinned against `Symbol` so this stops testing a hypothetical the day
        # someone changes the glyphs.
        self.assertIn(THREAT, Symbol.threat)
        self.assertIn(CARD, Symbol.ready_ally)


class TestUseUtf8Streams(unittest.TestCase):

    def test_it_reconfigures_stdout(self):
        stream, _ = Cp1252Stream()

        with mock.patch.object(sys, "stdout", stream):
            System.UseUtf8Streams()
            encoding = sys.stdout.encoding

        self.assertEqual(encoding, "utf-8")

    def test_it_reconfigures_stderr_too(self):
        # A traceback naming a card is the case that matters here, and it is
        # the one a crash report is built from.
        stream, _ = Cp1252Stream()

        with mock.patch.object(sys, "stderr", stream):
            System.UseUtf8Streams()
            encoding = sys.stderr.encoding

        self.assertEqual(encoding, "utf-8")

    def test_a_symbol_survives_a_reconfigured_stream(self):
        stream, raw = Cp1252Stream()

        with mock.patch.object(sys, "stdout", stream):
            System.UseUtf8Streams()
            print(THREAT + CARD, end="")
            sys.stdout.flush()

        self.assertEqual(raw.getvalue(), (THREAT + CARD).encode("utf-8"))

    def test_errors_stay_strict(self):
        # Not `errors="replace"`. `PYTHONIOENCODING=utf-8` -- pinned by
        # `tools/determinism/pinned_env.py` and by CI -- is strict, and a run
        # under the pin and a run without it must take the same path rather
        # than two that differ in how they mangle output.
        stream, _ = Cp1252Stream()

        with mock.patch.object(sys, "stdout", stream):
            System.UseUtf8Streams()
            errors = sys.stdout.errors

        self.assertEqual(errors, "strict")

    def test_a_stream_that_cannot_be_reconfigured_is_left_alone(self):
        # `sys.stdout` is None under pythonw, and a caller that has swapped in
        # a StringIO has already chosen its encoding by choosing memory.
        buffer = io.StringIO()

        with mock.patch.object(sys, "stdout", buffer):
            with mock.patch.object(sys, "stderr", None):
                System.UseUtf8Streams()
                buffer.write(THREAT)

        self.assertEqual(buffer.getvalue(), THREAT)

    def test_calling_it_twice_changes_nothing(self):
        stream, raw = Cp1252Stream()

        with mock.patch.object(sys, "stdout", stream):
            System.UseUtf8Streams()
            System.UseUtf8Streams()
            print(THREAT, end="")
            sys.stdout.flush()

        self.assertEqual(raw.getvalue(), THREAT.encode("utf-8"))

    def test_the_process_streams_were_configured_on_import(self):
        # The call is at the bottom of `core/utility/system.py`, so importing
        # anything in this repository has already run it. Skipped when the
        # runner has replaced stdout with a stream that has no encoding to set
        # -- `python -m unittest -b` does exactly that.
        if not hasattr(sys.stdout, "reconfigure"):
            self.skipTest("stdout has been replaced by the test runner")

        self.assertEqual(sys.stdout.encoding, "utf-8")


class TestTheLoggerCarriesTheLine(unittest.TestCase):
    """The acceptance criterion: the whole line arrives, not an error about it.

    `Log.all_log_text` is global and accumulates every line ever printed, so it
    is restored rather than left grown.
    """

    def setUp(self):
        self.addCleanup(setattr, Log, "all_log_text", Log.all_log_text)

    def test_a_symbol_bearing_line_reaches_stdout_whole(self):
        stream, raw = Cp1252Stream()
        line = f"Rhino {THREAT} 3 threat {CARD}"

        with mock.patch.object(sys, "stdout", stream):
            System.UseUtf8Streams()
            Log.Print(line)
            sys.stdout.flush()

        written = raw.getvalue().decode("utf-8")
        self.assertEqual(written, line + "\n")

    def test_the_logger_no_longer_swallows_the_line(self):
        # The old shape: `PrintUtf8` caught the encode error and printed
        # `Error printing log: ...` in place of the line. Nothing may replace a
        # log line with a report about it, because `Log.HasError` -- the corpus
        # gate -- reads counts, not text, and would never see it.
        stream, raw = Cp1252Stream()

        with mock.patch.object(sys, "stdout", stream):
            System.UseUtf8Streams()
            Log.Print(THREAT)
            sys.stdout.flush()

        self.assertNotIn("Error printing log", raw.getvalue().decode("utf-8"))

    def test_an_unconfigured_stream_would_have_failed_this(self):
        # The mutation, run as a test: without `UseUtf8Streams` the same line
        # raises out of the logger. That is what makes the two tests above
        # evidence rather than decoration -- and it is why the fallback had to
        # go with the fix, not stay alongside it.
        stream, _ = Cp1252Stream()

        with mock.patch.object(sys, "stdout", stream):
            with self.assertRaises(UnicodeEncodeError):
                Log.Print(THREAT)
                sys.stdout.flush()


if __name__ == "__main__":
    unittest.main()
