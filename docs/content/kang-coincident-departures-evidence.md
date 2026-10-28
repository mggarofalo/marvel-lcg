# Kang coincident local departures: prepared implementation

MARVEL-858 under MARVEL-568. Base `3f5ee5addc6d5b112e43d06b04c2e1c279847ce7`.
This bounded nonpublic packet implements multiple local departures due at the
same phase ending. It changes no production ability rows, admission, recipes,
protocol, UI or persisted fields. Windows focused verification is complete;
normal commit hooks and independent review are reported separately.

RR 1.8 `rr:first-player.3` governs: "If two or more effects would resolve
simultaneously, the first player decides the order in which to resolve them."
`rr:delayed-effect.1` puts these effects at their specified phase ending before
responses, and `.1.1` gives them constant-effect priority. The narrower
`rr:simultaneous-resolution` entry concerns shared bold timing triggers; it is
not the justification for ordering these delayed instructions. Vendored Kang
rulings and MarvelCDB FAQ were checked; none supplies a different co-due policy.

Printed `11002`–`11005` and expert `11035`–`11038` remove their matching 3B
on defeat and join another area at phase end. Printed `11009b`–`11012b` store a
Dominion immediately on completion, then remove their own villain/stage and
combine with another area at phase end. `pack:mc11:game-areas` moves the whole
combined group, including side schemes and engaged minions. The insert's unique
clarification requires reconciling matching physical copies before combining.
`11008b` permits central reunion only after other areas are gone; the absence
of an active local 3B does not erase its player group. `11013a/b` then reveal
III and stored Dominions and put physical associated nemeses into play.

The first player explicitly selects the next due local departure whenever more
than one remains. That selected effect resolves completely, including required
destination and repeated unique-copy choices, before another departure starts.
Completion removal therefore follows the selected effect's order. The final
singleton effect needs no ordering answer. Only a committed managed join extends
a remaining capture in its chosen destination with the newly arrived play areas;
the original source, defeated-card incarnation and completion capture remain.
Arbitrary topology changes still fail captured-group validation. Offer sorting,
typed batch bookkeeping and deterministic seat ordering are engine choices.
All nested frames retain the exact phase owner and originating occurrence.

Prepared tests use the existing actual-card overlay for two, three and four
groups, normal/expert, defeated/completed/mixed locals, nonzero first player,
both phase endings, reverse capture-order selection, precombined players,
unique-copy suspension, repeated-query and rejected-answer state/RNG/pending-work
purity, deterministic reconstruction, central entry once, and later readying.
Windows verification used `MSBUILDDISABLENODEREUSE=1`, serial `-m:1`, a
nonincremental Release Content test-project build and the complete
`FullyQualifiedName~Kang` filter. The initial build passed with zero warnings or
errors, followed by 148/148 passing Kang cases. Two separately compiled mutants
each failed all ten characteristic cases: resolving the first captured departure
instead of the selected effect, and extending the remaining capture in the old
source instead of the committed destination. The former exposes wrong departure
identity/destination offers; the latter exposes the missing whole-group extension.
Sources were restored before another nonincremental build and 148/148 restored
Kang cases passed. No output-lock warnings occurred.

Raw logs are outside the repository at
`C:/Users/mggar/Source/evidence/kang-coincident-*`: `build.log`, `focused.log`,
`mutant-order-build.log`, `mutant-order.log`, `mutant-group-build.log`,
`mutant-group.log`, `restored-build.log` and `restored-focused.log`.

Eliminated local seats, initial-split co-due interactions and competing registered
phase-end effects remain explicit unsupported boundaries. Complete pack
admission, public setup/session/replay, native interaction, Linux and full
managed gates are outside this packet.
