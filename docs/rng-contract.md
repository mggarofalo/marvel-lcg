# The random number generator cross-engine contract

Tracked as `MARVEL-38`. This document is the specification;
`src/Marvel.Core/Random/` implements it.

**It is written to be implementable from this document alone.** Where a choice
was arbitrary it says so and pins it anyway: an arbitrary choice made the same
way every time is what makes a seed name a game.

## Why this exists

Replays are a seed plus a list of player inputs. Replaying re-executes the
inputs against a generator re-seeded to the same value. If the two engines'
generators diverge by one draw, every shuffle after that point differs, the
state digest diverges, and the corpus reports a failure that has nothing to do
with the rules code being tested.

The alternative — recording every random outcome in the corpus — was considered
and rejected under `MARVEL-25`. It bloats the corpus, and it lets the C# engine
pass the oracle while having a broken generator, which is exactly the bug you
least want to ship.

## What was here before

Two generators, neither of them a contract.

`engine/lib/random.py` dispatched on a config flag, `disable_numpy_random`,
which defaulted to `False`. So the production generator was **numpy's legacy
global `RandomState`**, and the hand-written `engine/lib/mt19937.py` was dead
code that only ran if you set the flag.

Both are MT19937. They agree on the raw 32-bit stream and diverge only in how
they consume it:

| | numpy legacy `RandomState` | the repo's `mt19937.py` |
|---|---|---|
| first double from seed 42 | `0.3745401188473625` | `0.37454011430963874` |
| words per double | 2 (53-bit) | 1 |
| bounded integer | proper masked rejection | `int(random() / (1/(b-a)) + a)` |
| shuffle | Fisher-Yates | `10 * len(X)` random swaps |

The repo's third value equals numpy's second, which is the two-words-versus-one
divergence showing through. Neither consumption layer is worth porting: numpy's
legacy `RandomState` is a large surface to reproduce bit-exactly in C#, and the
repo's is simply wrong — `10 * len` random swaps is not a uniform permutation,
and truncating a float division has modulo bias and depends on cross-language
float semantics.

So both are replaced. The core stays MT19937, because it is a published
algorithm with published test vectors and because the existing raw stream
already matches it.

## 1. The core generator

Standard MT19937, 32-bit, exactly as published by Matsumoto and Nishimura. No
variation.

### Constants

```
w  = 32          n = 624         m = 397        r = 31
a  = 0x9908B0DF
u  = 11          d = 0xFFFFFFFF
s  = 7           b = 0x9D2C5680
t  = 15          c = 0xEFC60000
l  = 18
f  = 1812433253
lower_mask = 0x7FFFFFFF          upper_mask = 0x80000000
```

State is `mt[0..623]`, each an unsigned 32-bit word, plus an index `0..624`.

**All arithmetic is on unsigned 32-bit values.** Every assignment into `mt` is
masked with `0xFFFFFFFF`. In C# use `uint`; the masks are then redundant but
harmless.

### Seeding — `init_genrand`, always

MT19937 defines two seeding routines: `init_genrand` (one 32-bit word) and
`init_by_array` (an arbitrary-length key). numpy picks between them by the type
of what you pass it. **This contract always uses `init_genrand`**, and the seed
is always reduced to one 32-bit word:

```
seed(s):
    mt[0] = s & 0xFFFFFFFF
    for i in 1 .. 623:
        mt[i] = (f * (mt[i-1] XOR (mt[i-1] >> 30)) + i) & 0xFFFFFFFF
    index = 624
```

`index = 624` means "exhausted", so the first extraction twists before
returning anything. Do not set it to 0.

Note `>> 30`, which is `w - 2`. This is the published constant and is easy to
mistype as `>> 31`.

Engine seeds come from `Random.RandomSeed()`, which draws from `1` to `2**31-2`
inclusive, so the reduction never actually truncates. A seed of `0` is legal
and produces a valid stream.

### Twist

```
twist():
    for i in 0 .. 623:
        x  = (mt[i] AND upper_mask) + (mt[(i+1) mod 624] AND lower_mask)
        xA = x >> 1
        if x is odd: xA = xA XOR a
        mt[i] = mt[(i + 397) mod 624] XOR xA
    index = 0
```

The `+` is addition, not `OR`. On these two masked operands they give the same
result, but write addition so a reader checking against the published algorithm
finds what they expect.

### Extract — `NextUInt32`

```
NextUInt32():
    if index >= 624: twist()
    y = mt[index]
    y = y XOR (y >> 11)
    y = y XOR ((y << 7)  AND 0x9D2C5680)
    y = y XOR ((y << 15) AND 0xEFC60000)
    y = y XOR (y >> 18)
    index = index + 1
    return y AND 0xFFFFFFFF
```

The `& 0xFFFFFFFF` in `y >> 11` (the `d` constant) is a no-op for 32-bit `y`
and is omitted here.

**Reference vector.** Seed 42, first six words:

```
1608637542  3421126067  4083286876  787846414  3143890026  3348747335
```

These are also numpy's, which is the check that the core is unchanged from what
the engine used to produce.

## 2. Bounded integers — `NextBelow(n)`

**There are no floating point numbers anywhere in this contract.** Bounded
integers come straight from the raw 32-bit output. This sidesteps every
cross-language question about double rounding and eliminates modulo bias.

`NextBelow(n)` returns a uniform integer in `[0, n)` for `1 <= n <= 2**32`.

```
NextBelow(n):
    mask = n - 1
    mask = mask OR (mask >> 1)
    mask = mask OR (mask >> 2)
    mask = mask OR (mask >> 4)
    mask = mask OR (mask >> 8)
    mask = mask OR (mask >> 16)
    loop forever:
        value = NextUInt32() AND mask
        if value < n: return value
```

Bitmask with rejection. `mask` is the smallest `2^k - 1` that is at least
`n - 1`. Expected draws per call is under 2 and worst case is unbounded in
theory but geometric in practice.

Three properties worth stating because they are easy to get wrong:

- **There are no special cases.** `n = 1` gives `mask = 0`, so the first draw
  is `0 AND 0 = 0 < 1` and returns immediately. It still consumes one word.
  Do not short-circuit it — the stream position must match.
- **A rejected draw is consumed.** The rejected word is gone. An implementation
  that peeks or reuses it produces a different stream.
- **Powers of two are not special-cased either.** They simply never reject.

> **`n` does not fit in 32 bits.** The upper bound is `2**32`, which is one
> more than `uint.MaxValue`. Take `n` as a **64-bit** value — `ulong` or `long`
> in C#, not `uint`. A `uint` parameter wraps `n = 2**32` to `0`; `mask`
> coincidentally still comes out `0xFFFFFFFF`, but `value < n` becomes
> `value < 0`, which is never true on an unsigned type, and the loop **spins
> forever**. Nothing currently covers `n = 2**32`, so this fails by hanging
> rather than by returning a wrong answer, which is the worst way to find it.
> The comparison and the mask stay 32-bit; only the parameter widens.

Rejected as alternatives: Lemire multiply-shift (fewer draws, but the
consumption pattern is harder to specify unambiguously across languages), and
modulo (biased).

## 3. Shuffle — Fisher-Yates, downward

```
Shuffle(list):
    for i from len(list) - 1 down to 1:
        j = NextBelow(i + 1)
        swap list[i] and list[j]
```

**Downward.** Upward and downward Fisher-Yates both produce uniform
permutations, but from the same stream they produce *different* permutations,
so the direction is part of the contract.

`j` may equal `i`; that is a real outcome, not a wasted draw. A list of length
0 or 1 consumes nothing.

Total draws for length `L` is `L - 1`, plus rejections.

## 4. Choice

### `Choice(sequence)` — one element

```
Choice(sequence):
    return sequence[NextBelow(len(sequence))]
```

One draw plus rejections. The sequence must not be empty.

### `ChooseWithoutReplacement(sequence, k)` — k distinct elements

Partial Fisher-Yates, **upward**, over a copy:

```
ChooseWithoutReplacement(sequence, k):
    pool = copy of sequence
    result = []
    for i from 0 to k - 1:
        j = i + NextBelow(len(pool) - i)
        swap pool[i] and pool[j]
        append pool[i] to result
    return result
```

Exactly `k` draws plus rejections. Note this loop runs upward while `Shuffle`
runs downward; that is deliberate, because a partial shuffle has to fill from
the front. They are different functions and the difference is observable.

For `k = 1` this reduces to exactly `Choice`: same result, same one draw. So
the two are consistent and neither needs to special-case the other.

**Range and errors.** `k` must satisfy `0 <= k <= len(sequence)`. `k = 0`
returns an empty list and consumes nothing. Anything outside that range is an
error and must be rejected, not clamped — the Python raises `ValueError`. The
fixture cannot check this for you (it only records successful calls), so it is
stated here instead: an implementation that silently clamps `k` would pass
every vector and then quietly produce the wrong number of targets in a game.

**The engine layer adds one short-circuit**, described in section 6.

## 5. State capture

```
GetState() -> (mt[0..623] copied, index)
SetState(state) -> restores both
```

That is the whole generator state; nothing else affects future output. The copy
matters — handing out a reference to the live array makes a "snapshot" that
changes underneath the holder.

C# note: a `uint[624]` plus an `int`. Serialise as-is if you need to; there is
no canonical wire form because nothing crosses the wire.

## 6. What the engine calls

`engine/lib/random.py` is the facade the game uses. It holds one module-level
generator, seeded from the scene.

| Engine call | Contract function |
|---|---|
| `Random.SetSeed(seed)` | `seed(seed)`, and clears the undo history |
| `Random.RandomChoice(seq)` | `Choice(seq)` |
| `Random.RandomChoice2(seq, x)` | `ChooseWithoutReplacement(seq, x)`, but see below |
| `Random.Shuffle(list)` | `Shuffle(list)` in place |
| `Random.Undo()` | restores the state captured before the most recent draw |

**`RandomChoice2` short-circuits when `x == len(seq)`.** Selecting every element
is not a random choice — there is one answer — so it returns the input order and
**consumes no randomness**. This is inherited behaviour and it is kept
deliberately: it is semantically right, and changing it would move the stream
for no benefit. C# must reproduce it.

`x = 0` returns an empty list and consumes nothing. `x > len(seq)` and `x < 0`
are errors.

`Rand` in `game/operate/rand.py` wraps these again for the `disable_shuffle`
debug rule, which bypasses the generator entirely. That is a debug affordance,
not part of this contract, and it must never be on during corpus generation.

## 7. Undo

`Random.Undo()` exists for one debug console command (`Unshuffle`). It restores
the generator to its state before the most recent draw.

`MARVEL-7` found the previous implementation unsound and `MARVEL-26` found it
unbounded: every `choice` and `shuffle` appended a full state snapshot to a list
that only `Undo()` ever popped, so a long game grew it without limit and undo
semantics depended on total call history.

This contract bounds it. The generator keeps the last `UNDO_DEPTH` (32)
snapshots in a ring; older ones are discarded. `Undo()` past the end is an
error, not a silent no-op. `SetSeed` clears the ring.

**This is a debug affordance and is not part of the cross-engine contract.**
C# need not implement it. It is specified here only so that the Python's memory
behaviour is written down somewhere.

## 8. Test vectors

**There are none at present.** `datasets/rng/vectors.json` was generated by the
Python engine and went with it, and with it went the only independent check
that this is really MT19937 rather than something that merely looks
deterministic.

What should replace it is not another recording but the **standard published
test vector** — Matsumoto and Nishimura's own, which every conforming
implementation reproduces. That is roughly ten lines and is nobody's opinion.
Tracked as MARVEL-251.

Whatever replaces it should keep the fixture's one good idea: cover each
function independently *and* a mixed sequence that interleaves them, so a
per-function match cannot hide a stream-position error.

Whatever provides them should carry **no timestamp and no version stamp**. A
fixture is compared whole, so anything in it that churns for an unrelated reason
turns the staleness check into noise. Provenance belongs where it cannot go
stale — this document and `git log`.

## 9. Choices this specification makes

These change game outcomes rather than internals, and no rule decides any of
them. They are **ours**, and they are pinned rather than derived.
- **`disable_numpy_random` is gone**, along with the numpy dependency.
- **`Shuffle` is a real Fisher-Yates**, not `10 * len` random swaps. The old
  one did not produce uniform permutations.
- **Bounded integers no longer go through a float.**
- **`Undo()` is bounded and errors past the end** instead of growing without
  limit and silently doing nothing.

## 10. Notes for the C# implementer

- Use `uint` for the state and all intermediates. The masks in section 1 become
  no-ops; keep the shifts exactly as written. **The one exception is
  `NextBelow`'s `n`**, which must be 64-bit — see the box in section 2.
- `>>` on `uint` in C# is a logical shift, which is what this contract means
  everywhere. Do not use `int` — its `>>` is arithmetic and will differ on the
  high bit.
- `NextBelow` is the only place the stream can consume a variable number of
  words. If a cross-engine diff shows the stream drifting apart part-way
  through a game, this is the first place to look.
- The engine seeds once per game, from the scene. It does not reseed mid-game.
