# Quake occurrence actor binding — MARVEL-777

Base: `27a00397`. This packet changes actor binding and focused tests only.
Quake (`08012`) remains absent from the authored ability book and public admission.

## Authority and observed gap

The generated `datasets/cards/cards.json` prints: “After a minion schemes,
exhaust Quake → deal 2 damage to that minion.” The vendored FAQ for `08012`
confirms that a minion with zero SCH still schemes in alter-ego form.
`rr:leaves-play.1` says a departed card “is considered to be a new copy of the
card.” The effect must therefore retain the scheming incarnation.

The normal-ordering reproduction uses a test-only Quake row and a synthetic
earlier Response which discards the actor, independently selects Taskmaster
from the encounter discard pile, and puts that physical card into play.
Both abilities run through ordinary `Sequence.Answer`/`Sequence.Work` ordering.
Before the fix, Quake is offered again against the new copy. This proves the
engine composition gap; it does not establish an admitted printed card path
that performs this synthetic discard/reentry combination.

## Change and distinguishing observations

`OccurrenceCard` captures the participant incarnation. Its runtime-only property
is ignored by JSON and rebuilt by accepted-decision replay. Existing participant
kind/controller facts continue to describe the historical occurrence.
`trigger.actor` resolves no card after that captured copy leaves play. A manually
constructed occurrence without captured incarnation retains its existing lookup
behavior; scheduled actor occurrences supply the snapshot.

The five focused cases distinguish an unavailable response before payment from
an accepted response whose target subsequently disappears:

- An earlier accepted Response reenters the actor, suppressing Quake's offer.
- Invalid and stale answers preserve the digest, RNG and relevant pending work;
  a retained authoritative prompt rejects a changed actor before exhaustion.
- Ordinary zero-SCH minions at either seat take exactly two damage; Quake's
  controller at seat 1 answers and exhausts, while another minion is untouched.
- A synthetic forced damage interrupt reenters the target after Quake is
  accepted. The cost remains paid and the new incarnation takes no damage.
- Repeated discovery preserves digest, RNG, current pending step and stage.
  The runtime incarnation is absent from serialized participant facts.

## Verification

External logs are under `C:/Users/mggar/Source/`:

| Log | Result |
| --- | --- |
| `quake-actor-red.log` | Initial normal-ordering reproduction fails on the offered Quake response. |
| `quake-actor-mutant.log` | Compiled binding-bypass mutant fails that same distinguishing assertion; other three then-present cases pass. |
| `quake-actor-restored-build.log` | Sources restored in `finally`, followed by a successful nonincremental content-project rebuild. |
| `quake-actor-restored-focused.log` | Restored focused content regression passes 42/42. |
| `quake-actor-final-focused.log` | Final five Quake cases plus existing attack binding, threat response binding, window payment atomicity, combat response and Widow encounter regression pass 43/43. |
| `quake-actor-rules-regression.log` | Existing attack occurrence, lifecycle occurrence and rule citation checks pass 18/18. |

This is focused Windows engine evidence. Independent review and combined
integration/wire-history verification belong to the integrated candidate;
neither full-suite certification nor Quake content admission is claimed here.
