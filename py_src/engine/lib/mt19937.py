"""MT19937, specified in `docs/rng-contract.md` and shared with the C# engine.

The contract is the document, not this file. If the two disagree, the document
is right and this is a bug -- `datasets/rng/vectors.json` is what settles it.

Nothing here touches floating point. Bounded integers come straight off the raw
32-bit stream by masked rejection, which is what makes the same seed produce the
same game in a language with different float semantics.
"""

from core import *

MASK32 = 0xFFFFFFFF

# Standard MT19937-32 parameters. See `docs/rng-contract.md` section 1.
N = 624
M = 397
MATRIX_A = 0x9908B0DF
UPPER_MASK = 0x80000000
LOWER_MASK = 0x7FFFFFFF
INIT_MULTIPLIER = 1812433253

# `Random.Undo()` is a debug console affordance, so the history is bounded
# rather than unbounded: before MARVEL-38 every draw appended a full state
# snapshot to a list only `Undo()` popped (MARVEL-26). Deep enough for the
# console, shallow enough that a long game does not notice.
UNDO_DEPTH = 32


class Mt19937:
    """A seeded MT19937 stream. One instance per game."""

    def __init__(self, seed: int = 0) -> None:
        self.mt: List[int] = [0] * N
        self.index: int = N + 1
        self.history: 'collections.deque[Tuple[List[int], int]]' = \
            collections.deque(maxlen=UNDO_DEPTH)
        self.Seed(seed)

    ################################################################################
    # Core
    def Seed(self, seed: int) -> None:
        """`init_genrand`. The contract never uses `init_by_array`."""
        self.mt[0] = seed & MASK32
        for i in range(1, N):
            previous = self.mt[i - 1]
            # `>> 30` is `w - 2`, the published constant. Not 31.
            self.mt[i] = (INIT_MULTIPLIER * (previous ^ (previous >> 30)) + i) & MASK32
        # "Exhausted", so the first extraction twists before returning.
        self.index = N
        self.history.clear()

    def Twist(self) -> None:
        for i in range(N):
            x = (self.mt[i] & UPPER_MASK) + (self.mt[(i + 1) % N] & LOWER_MASK)
            xa = x >> 1
            if x & 1:
                xa ^= MATRIX_A
            self.mt[i] = self.mt[(i + M) % N] ^ xa
        self.index = 0

    def NextUInt32(self) -> int:
        """One raw 32-bit word. Every other method is built from this."""
        if self.index >= N:
            self.Twist()

        y = self.mt[self.index]
        y ^= (y >> 11)
        y ^= (y << 7) & 0x9D2C5680
        y ^= (y << 15) & 0xEFC60000
        y ^= (y >> 18)

        self.index += 1
        return y & MASK32

    ################################################################################
    # Bounded integers
    def NextBelow(self, n: int) -> int:
        """Uniform in `[0, n)`, by masked rejection. No floats, no modulo bias.

        `n = 1` is not special-cased: the mask is 0, the first draw returns 0,
        and one word is consumed. Short-circuiting it would move the stream.
        """
        if n < 1:
            raise ValueError(f"n must be at least 1, got {n}")
        if n > 0x100000000:
            raise ValueError(f"n must fit in 32 bits, got {n}")

        mask = n - 1
        mask |= mask >> 1
        mask |= mask >> 2
        mask |= mask >> 4
        mask |= mask >> 8
        mask |= mask >> 16

        while True:
            value = self.NextUInt32() & mask
            if value < n:
                return value

    ################################################################################
    # The operations the game performs
    T = TypeVar("T")

    def Shuffle(self, items: List[T]) -> None:
        """Fisher-Yates, downward, in place. `len - 1` draws."""
        if len(items) < 2:
            # No draw, so nothing to rewind to. Recording one anyway would
            # spend a slot in the bounded ring on a call that did nothing.
            return
        self.PushState()
        for i in range(len(items) - 1, 0, -1):
            j = self.NextBelow(i + 1)
            items[i], items[j] = items[j], items[i]

    def Choice(self, sequence: Sequence[T]) -> T:
        """One element, uniformly. One draw."""
        if len(sequence) == 0:
            raise ValueError("cannot choose from an empty sequence")
        self.PushState()
        return sequence[self.NextBelow(len(sequence))]

    def ChooseWithoutReplacement(self, sequence: Sequence[T], k: int) -> List[T]:
        """`k` distinct elements: partial Fisher-Yates, upward. `k` draws.

        Upward, unlike `Shuffle`, because a partial shuffle fills from the
        front. For `k = 1` this is exactly `Choice` -- same element, same draw.
        """
        if k < 0:
            raise ValueError(f"k cannot be negative, got {k}")
        if k > len(sequence):
            raise ValueError(f"k cannot exceed the sequence length, got {k} of {len(sequence)}")
        if k == 0:
            return []

        self.PushState()
        pool = list(sequence)
        result: List['Mt19937.T'] = []
        for i in range(k):
            j = i + self.NextBelow(len(pool) - i)
            pool[i], pool[j] = pool[j], pool[i]
            result.append(pool[i])
        return result

    ################################################################################
    # State
    def GetState(self) -> Tuple[List[int], int]:
        """A snapshot. The list is copied, so it does not move underneath you."""
        return (self.mt[:], self.index)

    def SetState(self, state: Tuple[List[int], int]) -> None:
        words, index = state
        if len(words) != N:
            raise ValueError(f"state needs {N} words, got {len(words)}")
        if not 0 <= index <= N:
            raise ValueError(f"state index out of range: {index}")
        self.mt = words[:]
        self.index = index

    ################################################################################
    # Undo -- debug console only, not part of the cross-engine contract
    def PushState(self) -> None:
        """Record the position before a draw, for `Undo`. Bounded ring."""
        self.history.append(self.GetState())

    def Undo(self) -> None:
        """Rewind to before the most recent draw.

        Raises rather than silently doing nothing: a debug command that
        quietly fails is worse than one that says it cannot.
        """
        if not self.history:
            raise IndexError(
                "nothing to undo: no draw has been made since the last seed, "
                f"or more than {UNDO_DEPTH} draws have happened since")
        self.SetState(self.history.pop())
