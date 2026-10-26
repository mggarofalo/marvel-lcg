# Hammer Throw recovery preflight packet

MARVEL-758 engine packet. Public Thor admission remains closed.

The labelled attack projection retains an opaque recovery boundary rather than
predicting an encounter draw. Its accepted operation is a cost-free mandatory
self interrupt: discard one encounter card, test that discarded receipt for
treachery, and heal that same minion's damage. Other replacement bodies, optional
interrupts and additional defeat interrupts remain unsupported.

One immediate return of an exactly selected owned in-play upgrade may follow.
Dependency analysis traces the recovering minion's healing and complete hosted
departure tree through the existing constant-field dependency queries. Fixed
health grants (including actual Thor's Helmet and Endurance) and direct lasting
health effects coexist safely. A disappearing health source that affects another
character, a health predicate that can change during recovery, dynamic health
amounts, dynamic health recipients and registered trait-scoped health remain
unsupported. Unknown constant operations still refuse. There is no title or card
id allowlist; the ordinary title selector identifies the physical return target.
Actual Genetically Enhanced (01163) on Loki is covered: its +3 hit points give
zero spill on recovery and one spill on actual defeat, with attachment departure
only for that defeat. Biomechanical Upgrades (01185) adds a second recovery body
and still raises atomically. Zola attachment defeat bodies are not claimed here.

The returned upgrade's departure uses the existing live constant-ending preflight
and settlement, including attachments and cascaded health loss. A synthetic
AERIAL-dependent health constant proves both survival and elimination after
Mjolnir returns. Printed Thor's Helmet itself has unconditional +5 hit points;
it does not depend on AERIAL. Biomechanical Upgrades on Loki remains a relevant
unsupported replacement, rather than being treated as harmless topology.

Authority is printed 06005/06009/06010/06028 and 05023 in
`datasets/cards/cards.json`, Loki's mandatory interrupt FAQ erratum,
`rr:damage.step.6`, `rr:damage.step.9`, `rr:hit-points.2.3` and
Rules Reference 1.8 `rr:overkill.1`. Overkill requires an actual defeat: Loki's
treachery recovery yields zero villain damage; actual defeat yields four.
Older Loki overkill timing rulings do not override that current rule.

This packet is isolated on `d7fcbf70`; it excludes rejected Rules source
8265a038/80814897. The rebased original Cards packet is `1d7d41e1`.
Focused evidence and mutation/restoration logs live outside the repository under
`C:/Users/mggar/Source/evidence/hammer-dependencies-*`.
The restored nonincremental build and 13 focused tests pass. A compiled mutant
that ignores changing conditional health fails the health-dependency refusal
test. The 129 architecture tests pass; normal commit hooks remain enabled.
No public setup/save/replay or native interaction claim is made.
