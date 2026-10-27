# Doctor Strange remaining public restart checkpoints

MARVEL-838 packet based on `eb6f428dad5560c0d61e89f3a51aa717ac0de109`.
The new `PublicDoctorStrangeRemainingRestartTests` and
`PublicStrangeCheckpointJourney` reuse the unchanged observing production factory
and strict fresh-host fixture. They never prepare or mutate a World, create
cards, replace authored abilities or alter setup. The decision policy uses
current engine offers and public identity information. Hidden piles are observed
only to assert the selected checkpoint and its outcome.

A finite harvest reused starter seeds 1 through 12, with at most 100 decisions
per journey. Games ended in rounds 2 through 5; no journey reached the cap.
The initial diagnostic policy incorrectly sought an End Turn affordance. The
corrected policy uses the actual cancellable root prompt's Decline command and
reruns the same twelve candidates. The diagnostic test is removed; final tests
use pinned seeds and do not search. Full diagnostic prompts and decisions remain
outside the repository in `C:/Users/mggar/Source/strange-remaining-harvest.log`.

Seed 2 naturally uses Natural Talent and successive Spell Mastery activations.
In round 4, Vapors is the final physical Invocation. Its first choice selects
the exact confused status; the restart occurs at the offered replacement choice.
The resumed answer replaces that status with tough on its original character,
leaves the other status intact, preserves the paid hand and exhausted identity,
and recycles all five owned physical Invocations. The exact Invocation owner and
incarnation survive reconstruction.

Seed 9 reaches Winds as the last Invocation in round 4. Before commitment, the
test clones the RNG state and independently shuffles the four physical discards
plus Winds in their required order. It compares the complete shuffle order in
the authoritative journal, current top, complete RNG state and consumed-word
delta. The player deck loses exactly its next three cards to the hand, retains
its remaining order, and receives no exhaustion encounter card. Repeated sync
and spectator projections preserve a baseline captured before probing, including
digest, full RNG state/count, pending prompt, outstanding agenda and durable
session. The fresh host supplies the independent unprobed continuation.

Both cases strictly serialize into a separate store and construct a new factory,
host, Game and World. They compare the complete accepted save, ordered events,
visible prompt/world/history, digest and RNG with uninterrupted continuation.
Owner-seat responses are compared exactly across reconstruction. Direct spectator
projections assert concealed future Invocation identities and private hands.
These are solo journeys; this packet does not claim
additional multiplayer-seat or native UI evidence.

The first focused run passed Vapors and both citation gates. Winds failed only
its independent cloned consumed-word counter assertion (clone 4 versus live 115).
`SetState` restores the generator words/index and does not import its lifetime
counter. The corrected oracle adds the captured baseline to the clone delta;
its state and shuffle-order comparisons are unchanged.

Open the Dark Dimension was not revealed by these twelve legal starter journeys.
No synthetic prepared factory is substituted for public acceptance. Its exact
facedown hosted Invocation and defeat-return fresh-host proof remains open.
The normal authored Special does not expose a separate decision between Master's
Special discard/reveal and return, so this packet does not invent such a pause.

An ordinary payment draft remains client-local until one complete commitment.
The existing public paid-Special cases reject incomplete unpaid answers without
changing durable state. Inspection of all authored Resource rows found direct
generation, top-discard generation, exhaustion and counter-removal costs; no
actual reachable resource-generator choice was established in a legal Strange
starter/custom deck. The Eye of Agamotto suspension test explicitly replaces
another card with a synthetic lethal generator cost and adds a synthetic defeat
interrupt. That proves an engine continuation contract, not a public Strange
mid-payment checkpoint. This packet makes no blanket claim that resource
suspension is unimplemented or required for ordinary draft persistence.

Evidence logs and TRX live outside the repository under
`C:/Users/mggar/Source/strange-remaining-evidence/` and sibling
`strange-remaining-*.log` files. Complete-game work, historical compatibility
integration, Plane updates and final MARVEL-838 reconciliation belong to the lead.
No production code, book, setup, schema, version or archive is edited here.

Final focused verification: both complete Strange restart classes and the citation
class passed 8/8 (`remaining2.trx`). A compiled test-harness mutant replaces each
fresh-host reconstruction with the original live host/store/factory tuple. Both
new tests fail specifically at `Assert.NotSame` (`same-host-mutant.trx`), so a
same-instance continuation cannot satisfy this proof. The new test source is
restored in a finally block. A nonincremental Release rebuild then succeeds with
zero warnings/errors, and the same complete selected classes pass 8/8 again
(`restored.trx`). This is targeted managed evidence, not a whole-suite or second
platform result. Normal pre-commit hooks remain enabled.

Independent review of `4a59592d42ff75741030ae3cdc9339be204c9aa4` accepts the two
claimed public restart paths. The observing factory delegates normal setup to
`DatasetGameFactory`; the pinned journeys submit accepted host decisions without
World preparation. The Winds oracle independently computes the required pile
order and RNG delta, while Vapors checks the selected physical status, original
host, committed hand/exhaustion and completed Invocation recycle. Exact prompt,
save, event, digest and RNG comparisons supplement those behavioral oracles.
The cloned RNG's counter starts at zero after `SetState`, so adding the captured
live baseline is required and does not weaken its full-state comparison.

The review adds `PublicUnpaidSpellMasteryRejectsAtomicallyAndLeavesTheValidCommitmentAvailable`.
At seed 2's paid hero-form Invocation, it removes the required resource generators,
asserts rejection with unchanged prompt, digest, full RNG state/count, agenda and
stored session, then syncs and completes the valid commitment against a fresh
unprobed control. The initial probe selected alter-ego Natural Talent and failed
its payment precondition; the corrected probe explicitly requires hero form and
a paid Invocation. The complete two restart classes plus citation class pass 9/9
in `C:/Users/mggar/Source/strange-remaining-review-evidence/review2.trx`.
This adds rejection evidence, not a requirement to persist an uncommitted draft.

The verdict excludes hosted Open the Dark Dimension return, multiplayer privacy,
native interaction and the separate generic captured-face presentation finding.
No claim of complete Doctor Strange acceptance follows from this packet.
