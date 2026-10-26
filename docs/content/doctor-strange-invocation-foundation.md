# Doctor Strange Invocation foundation — MARVEL-794

Parent: MARVEL-565. Base: `a02316b8`. This is the first bounded D1 engine
foundation, not complete D1 or Doctor Strange admission.

`InvocationDeck` binds a player's exact `09001a/b` identity to one physical
copy each of `09032–09036`. Initialization validates all membership, ownership,
source and identity constraints before creating areas, moving cards or using
RNG. It reuses the existing player-owned `AdditionalDeck` and
`AdditionalDiscardPile` zones and the world's seeded shuffle. It creates no
production ability rows, public recipes, admission exceptions or UI contracts.

The top stays concealed until every mulligan finishes. Discarding a bound
Invocation through the ordinary engine discard operation routes it to its own
discard, updates the public top and immediately recycles an empty deck without
calling the player-deck reset or dealing an encounter penalty. The typed
`DiscardTop` operation rejects deeper and foreign physical cards. This slice
does not introduce Special execution; unsupported ability behavior retains its
existing fail-closed boundary.

Authority is the complete pinned `pack:mc08:the-invocation-deck`, generated
identity/Invocation printed faces in `datasets/cards/cards.json`, and setup's
mulligan boundary. `ruling:70055fca52b1ae18` corroborates the after-mulligan
exposure. No Master timing, Vapors targeting or supersession claims are made;
their graph reconciliation remains necessary before their executable probes.

`InvocationDeckTests` covers seeded separate shuffle, existing v3 zone/owner
records, complete-mulligan visibility, separate discard and immediate recycle,
the absence of a player-deck penalty despite an empty ordinary deck and
populated ordinary discard, second-seat ownership, invalid setup atomicity,
foreign/deeper top rejection, repeated-query RNG/digest/prompt purity and
subsequent execution against an unprobed control. These are nonpublic synthetic
engine fixtures; they do not prove end-to-end View disclosure or public setup.

Next D1 step: use **Winds of Watoomb** as the first supported Special, captured
from the exact current top; prove **Spell Mastery** cost plus hero exhaustion
atomically, including zero-cost Winds. Then implement **Master of the Mystic
Arts** using the same physical Invocation: pay event and printed Invocation
costs, resolve/discard it, expose/recycle the next top, then return that original
physical card. Reconcile its current/older ruling supersession in the graph
before asserting that continuation. Preserve fail-closed handling of every
other unsupported Special. Natural Talent/Wong top-discard composition and D2
hosted-card removal/return still require their own authority-driven probes.

Public setup, complete face/quantity admission, replay/save compatibility,
persistent visibility-safe top contracts and Godot interaction remain later
pack work. Ordinary Core areas, RNG consumption and digest records are unchanged
when no Invocation lifecycle is initialized.

Validation on the foundation candidate:

- Full Release solution rebuild with `--no-incremental`: succeeded, zero warnings/errors.
- Full `Marvel.Rules.Tests`: 1,153 passed, including 14 Invocation cases.
- Full `Marvel.Core.Tests`: 51 passed, including the RNG and v3 digest contracts.
- Compiled foreign-world guard mutant: killed by the foreign-world setup case.
- Compiled player-deck reset mutant: killed by the separate-deck/no-penalty control.
- Restored sources rebuilt before the final passing Rules/Core runs.

Raw logs are outside the repository at
`C:/Users/mggar/Source/strange-invocation-{build,rules-restored,core,mutant-world,mutant-penalty}.log`.
Independent source review and broader integrated gates remain the lead's next
steps; no cross-platform or complete-pack acceptance is claimed here.

Independent review of `3a20ae07` found no blocking issue within this foundation.
Additional synthetic controls cover atomic rejection of either existing
player-owned additional area and retention of the physical binding through an
in-play departure/re-entry while copy state resets. A real-catalog View probe
checks spectator, both restricted seats and cooperative visibility through
mulligans, initial reveal, discards and recycling: deeper identities and shuffle
order remain concealed. This proves the existing projection's privacy on this
research board, not a complete public Invocation interaction contract.

The exact identity/five-face checks belong to this typed mechanic's validation;
they do not select content in the scheduler or transport. Future production
setup must still obtain the physical composition from the admitted catalog and
reconstruct this game-owned binding from the pinned setup and decisions. The
foundation does not make a digest alone sufficient to restore that binding.
