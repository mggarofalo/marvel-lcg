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

## Independent review of `5ae4efdfd7e9017ba9482b69bbb9f0452150e62d`

Reviewed in isolated branch `review/quake-incarnation`, worktree
`C:/Users/mggar/Source/marvel-quake-review`. No production correctness finding
was identified within the occurrence-capture and actor-binding change.
The review adds only tests and this evidence; ability-book, admission and
compatibility/version changes remain outside this packet.

`QuakeActorIndependentReviewTests` supplies three independent controls:

- Moving the scheming actor between in-play engagement areas preserves its
  incarnation and the retained Quake answer still exhausts Quake and damages
  that actor. This distinguishes a departed copy from ordinary board movement.
- A captured occurrence and an explicitly constructed legacy occurrence both
  offer Quake before reentry. Afterwards only the legacy occurrence without a
  captured incarnation offers it; historical minion/controller facts remain
  unchanged. The missing-incarnation fallback is deliberate, not a claim that
  deserialized occurrence JSON is suitable for resuming a live window.
- Different runtime incarnations produce exactly identical participant JSON.
  Deserialization retains the historical public facts and omits incarnation;
  the complete occurrence JSON is identical to the equivalent historical value.

Independent Windows evidence, with all logs outside the worktree:

| Log under `C:/Users/mggar/Source/` | Result |
| --- | --- |
| `quake-independent-content.log` | 68/68: three independent controls, five supplied Quake cases, attack/threat binding, payment atomicity, combat responses, Widow encounter composition, typed activation targeting, Jet Trooper and boost cancellation. |
| `quake-independent-rules.log` | 18/18: attack/lifecycle occurrences and rule citations. |
| `quake-independent-history.log` | 14/14: journal records, capture isolation and replay history activity. |
| `quake-independent-mutant.log` | Binding-bypass mutant compiles; captured actor reentry control fails at `Assert.Empty`, while the other two independent controls pass. |
| `quake-independent-restored-build.log` | Successful nonincremental content-project rebuild after restoring the original source bytes in `finally`. |
| `quake-independent-restored-content.log` | Restored 68/68 content checks pass. |

The public facts' JSON representation is unchanged, and authoritative session
reconstruction uses setup plus accepted decisions rather than loading mutable
occurrence objects. Correcting actor eligibility can nevertheless change prompts
or effects during replay. The journal/history regressions above do not establish
compatibility for a genuine predecessor trace reaching that changed behavior;
the integrated replay/version decision remains separate.

The normal accepted response and accepted child-damage reentry paths are proved
with synthetic abilities. No actual printed discard/reentry card combination,
Quake admission, full game, native UI, cross-platform certification or complete
managed-suite certification is claimed by this review.
