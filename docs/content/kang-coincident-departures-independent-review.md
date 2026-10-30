# Independent review: coincident Kang departures

Reviewed source `3f4dbc0a571deaccb1fdc8c89a82a8c1dbbd47d9` against base
`3f5ee5addc6d5b112e43d06b04c2e1c279847ce7`. Verdict: **changes requested**
for the claimed fail-closed boundary. This is a bounded nonpublic capability
review, not complete Kang admission or product acceptance.

## Finding: a retained group can change while another join is suspended

`KangJoinProcedure.RequireOwner` validates the selected departure's group but
not the captures in `Following`. With two due departures, select the first,
select the second's group as destination, and stop at the matching unique-card
choice. If an uncontrolled topology change adds a third player to the retained
destination group, answering the unique choice still commits a discard. The
next `Sequence.Work` throws when it validates the now-stale remaining capture,
after the discard has already changed state. This contradicts the packet's
claim that arbitrary topology changes fail captured-group validation without
committing a choice. The topology change is synthetic; no claim is made that
ordinary admitted gameplay can cause it during this prompt.

A focused regression with nonempty player decks expected refusal before the
answer and failed because no exception was thrown. Its source is preserved at
`C:/Users/mggar/Source/evidence/kang-coincident-independent-regression.cs`,
and its build/test output at `C:/Users/mggar/Source/evidence/kang-coincident-independent-regression.log`.
A separate characterization asserted the discard occurred and the subsequent
work raised; it passed (1/1), with output at
`C:/Users/mggar/Source/evidence/kang-coincident-independent-characterization.log`.
The failing regression is kept outside the repository pending a production fix.
Validate all retained captures before committing choices and joins, while still
allowing the specifically managed destination extension, then run the regression.

## Reviewed evidence and limits

RR 1.8 `rr:first-player.3` explicitly assigns simultaneous-effect ordering to
the first player. `rr:delayed-effect.1` and `.1.1` establish the phase-end
resolution and priority. The source's two-to-four-player actual-card overlay
exercises reverse ordering, normal/expert, defeated/completed/mixed captures,
nonzero first player, precombined groups, unique choices, both phase endings,
and central entry once. The packet reports 148/148 focused Kang cases, two
compiled negative controls with ten failures each, and a restored passing run;
those are source-author evidence, not independently rerun by this review.
Prompt query/rejection tests include state, topology, RNG and pending agenda;
exact owner/occurrence is asserted while choices are pending. Existing separate
boundary tests reject changes to the selected source. The probe above tests a
distinct retained source during suspension.

Complete Kang, eliminated players, competing delayed effects, public session or
replay, native interaction, Linux, and broad managed gates remain outside this
review. A test result does not establish product readiness.

## Closure review of repair `64dc045f198d2d05074a421c51ca579cdaa5ca22`

**Verdict: the retained-group finding above is closed for this bounded
capability.** The original rejection remains the record of the reviewed source.
The repair checks every retained capture before an order or join choice commits,
before completed local cards are removed, and before scheduling. The join path
validates again before producing a prompt or moving the selected group. After a
managed join, only the capture at that destination is extended and the updated
batch is validated, preserving the permitted whole-group continuation.

The imported regression matches the original external probe after normalizing
line endings. The raw working-tree hashes differ because this Windows checkout
uses CRLF; the source evidence's unqualified identical-SHA256 statement does not
hold for those working-tree bytes. The regression now refuses the unique discard
with state, topology, events, agenda and RNG unchanged. Three additional
synthetic cases cover a different retained source during destination selection,
and work before a unique prompt or a physical join. Existing actual-card
multi-group cases exercise managed rebase and central reunion.

An independent Windows Release build and `FullyQualifiedName~Kang` run passed
152/152 at the repaired source; log:
`C:/Users/mggar/Source/evidence/kang-retained-independent-built-closure.log`.
The preceding `--no-build` attempt used a stale 148-test assembly and is not
closure evidence. The source-author selected-only mutant failed all four retained
cases, followed by restored 152/152; its logs are linked in the repair evidence.
No additional control was needed for the identified boundary. The scope and
unverified product, platform and other timing boundaries above remain unchanged.
