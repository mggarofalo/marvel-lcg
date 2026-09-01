# Random number generator contract

`src/Marvel.Core/Random/MersenneTwister.cs` implements the gameplay RNG. This
document fixes the algorithm and every consumption rule that can change a seeded
game.

A seed plus the same ordered decisions must produce the same game on every
supported platform. Gameplay uses no other RNG, floating-point random value,
wall-clock seed or ambient random source.

## Generator

The generator is `std::mt19937` as defined by ISO/IEC 14882 §rand.predef. It has
624 unsigned 32-bit state words and one index.

| Name | Value |
|---|---:|
| `w` | 32 |
| `n` | 624 |
| `m` | 397 |
| `r` | 31 |
| `a` | `0x9908B0DF` |
| `u` | 11 |
| `d` | `0xFFFFFFFF` |
| `s` | 7 |
| `b` | `0x9D2C5680` |
| `t` | 15 |
| `c` | `0xEFC60000` |
| `l` | 18 |
| `f` | `1812433253` |

All arithmetic in the state transition wraps modulo `2^32`.

## Seeding

The public seed is one unsigned 32-bit integer. The generator uses the standard
`init_genrand` recurrence:

```text
mt[0] = seed
for i = 1 to 623:
    mt[i] = f * (mt[i - 1] xor (mt[i - 1] >> 30)) + i
index = 624
wordsConsumed = 0
```

The contract does not implement MT19937’s separate arbitrary-length key
initialization procedure. Reseeding during a game is not allowed.

## Twist

When `index >= 624`, refill the state:

```text
lowerMask = (1 << 31) - 1
upperMask = bitwise-not lowerMask within 32 bits

for i = 0 to 623:
    x  = (mt[i] and upperMask) + (mt[(i + 1) mod 624] and lowerMask)
    xA = x >> 1
    if x is odd:
        xA = xA xor a
    mt[i] = mt[(i + 397) mod 624] xor xA

index = 0
```

`upperMask` is `0x80000000`; `lowerMask` is `0x7FFFFFFF`.

## Extracting one word

`NextUInt32` twists when needed, takes `mt[index]`, increments the index and word
counter, then tempers in this order:

```text
y = y xor ((y >> 11) and 0xFFFFFFFF)
y = y xor ((y << 7)  and 0x9D2C5680)
y = y xor ((y << 15) and 0xEFC60000)
y = y xor (y >> 18)
```

The returned value is one unsigned 32-bit word. No gameplay API converts it to a
floating-point number.

## Bounded integers

`NextBelow(n)` returns a uniform integer in `[0, n)` by masked rejection.

The valid range is `1 <= n <= 2^32`. The parameter is 64-bit so `2^32` does not
wrap to zero.

```text
mask = smallest value of the form 2^k - 1 that is >= n - 1
repeat:
    value = NextUInt32() and mask
until value < n
return value
```

For `n = 1`, the result is zero but one word is still consumed. For `n = 2^32`,
the mask is `0xFFFFFFFF`, every word is accepted, and the result is the full
word.

Modulo reduction is not permitted because it biases values when `n` does not
divide `2^32`. Multiplying a random floating-point value by `n` is not permitted
because it changes consumption and introduces runtime-dependent rounding.

## Shuffle

`Shuffle` is in-place Fisher-Yates from the end of the list:

```text
for i = count - 1 down to 1:
    j = NextBelow(i + 1)
    swap(items[i], items[j])
```

Lists of length zero or one consume no words.

The loop direction, inclusive bound and swap-on-equality behavior are part of the
contract. Skipping a draw when `j == i` would move the stream.

## Choice

`Choice(sequence)` requires at least one element and returns:

```text
sequence[NextBelow(sequence.Count)]
```

It consumes according to `NextBelow`, including one word for a single-element
sequence.

## Choice without replacement

`ChooseWithoutReplacement(sequence, k)` requires
`0 <= k <= sequence.Count`.

- `k = 0` returns an empty list and consumes no words.
- Every positive value uses a partial Fisher-Yates selection without replacement.

The partial shuffle runs upward over a copy. It consumes one accepted word per
selected element, including the final selection from a pool of one. The output
order is the order selected by the algorithm. It is not sorted afterward.

`EngineRandom.Choice2` is the gameplay facade and adds one deliberate
short-circuit: choosing every element returns the input order and consumes no
words. The raw generator and gameplay facade therefore differ for this case,
and tests pin both contracts.

## State capture

`GetState()` returns a copy of all 624 state words and the current index.
`SetState()` restores both.

The returned words must be copied. A reference to the live array would not be a
snapshot. `WordsConsumed` is diagnostic state and does not influence future
output.

There is no canonical serialized RNG state because generator state does not
cross the game or client wire.

## Standard test vector

ISO/IEC 14882 §rand.predef requires `std::mt19937` seeded with `5489` to produce
`4123659995` on its 10,000th consecutive draw. This crosses 16 complete state
refills and catches twist errors that a short prefix can miss.

`MersenneTwisterTests` also pin:

- the first words from seed `5489`;
- seeds with the high bit set;
- bounded values that require rejection;
- bounds at 1 and `2^32`;
- deterministic Fisher-Yates permutations;
- choice without replacement at every boundary; and
- state capture and restoration.

These tests hold the implementation to a published algorithm, not to a recording
made by this engine.

## Engine rules

The engine seeds once when it creates a game. All shuffles and random choices
draw from that one stream in deterministic resolution order.

The `Choice2` all-elements short-circuit consumes no random word. Other
single-result generator calls still consume according to their contracts.
Iterating candidates from an unordered collection before a random selection is
forbidden because the same index could name a different game element.

Changing any algorithm, short-circuit or call order changes the game identified
by a seed. Treat such a change as a wire-format change and update the published
contract before the implementation.
