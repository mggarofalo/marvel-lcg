# Hulk obligation and nemesis actual-card review — MARVEL-849

Reviewed source `a07d4305f433a975bace10e1fe255c9b93cf5227` against predecessor
`b753c463a6baa3756a954d08db3ae40336c190d3`, generated printed cards, current
RR 1.8, local rulings and the implementation handoff. The review owns seven
additional actual-card cases and the lead-authored one-token correction to Clash.
These are nonpublic prepared engine scenes, not public Hulk admission or native
client acceptance.

The source has a concrete Clash target defect. Its `heroesAndAllies` query
includes alter-egos, while printed 10028 explicitly names a hero or ally.
`rr:form-change-form.5` says abilities interacting with a hero do not interact
with that identity in alter-ego form. With both identities in alter-ego and no
allies, full reveal offers Peter Parker and Carol Danvers instead of gaining
surge. The focused source failure is preserved in
`C:/Users/mggar/Source/evidence/hulk-nemesis-independent-clash-source-failure.log`.
The existing `heroAndAllyTargets` query supplies the required pool. The lead
authored that row correction; no engine change is required.

The new Clash cases cover the empty eligible pool and both valid mixed-pool
controls: an eligible hero or ally with modified ATK zero receives the attack
without offering alter-egos in the zero-ATK tie. They use the full encounter procedure and assert
damage recipient, surge deal, treachery discard and completed activation/agenda.
The retained source cases separately cover enemy/character ties decided by the
nonzero first player, modified ATK, exact selected ally, another defending ally,
stun cancellation and Tough prevention. The old-query mutant must fail the
empty-pool and both mixed-pool cases.

New Abomination cases distinguish the helping defender's player from the
original attacked player, an empty deck with empty discard, and a physical last
card whose immediate deck reset must preserve the response's discard receipt.
The helping player takes the separate two damage after their ally defends;
the other player's top card stays in place. The last-card case verifies the
reset deck, encounter deal and two damage even though the physical card is
already back in the deck. The source tests retain physical/wild/mental controls,
same-player ally defense, stun and complete attack drainage. A scoped compiled
mutant that requires the discarded physical card to remain in a discard pile
must fail the last-card case.

The new Total Destruction control leaves the locked scheme untouched while
actual printed To the Rescue pays and removes threat from another legal scheme.
This complements source positive/negative basic-thwart and event controls,
live Abomination entry/departure permission, per-player starting threat and
hazard. No concrete additional permission defect was found. Inner Demons'
corrected text and mandatory flip/resulting-form branch agree with its actual
source tests, including hand shortages, GiveTo routing to the non-first Hulk
player, exhaustion and obligation cleanup. The superseded All Tied Up answer
depends on the removed Then and does not govern the corrected printed text.

Evidence scripts and logs are outside the repository under
`C:/Users/mggar/Source/evidence/`, using the prefix
`hulk-nemesis-independent-`. The corrected focused run passes all seven new
cases (`corrected-probes.log`). The old-query mutant fails all three Clash
cases (`mutant-old-clash-query.log`); the compiled discard-zone mutant fails
the physical last-card case with 3 damage instead of 5 (`mutant-discard-zone.log`).
Both mutations are restored in `finally`; the nonincremental restored Release
build passes with zero warnings/errors (`restored-build.log`). The raw-object
check proves all 712 predecessor UTF-8 objects exact against base and source,
LF preserved, 716 total rows, 31 Hulk faces and absent 10015/10029/90001
(`prefix.log`). The source's 326-case
evidence and five data mutants remain earlier implementation evidence rather
than a claim that its original Clash selector was correct.

Final corrected-candidate verification passes all 333 affected content cases,
including seven new review cases, all Hulk cases, complete affected inventory
owners, AbilityData, ranked/selected activation classes and rule citations
(`affected-final.log`). All 129 architecture checks pass (`architecture.log`).
The two independent mutants are absent from the final sources. No engine,
setup, admission, protocol or presentation change is included. This review
accepts the corrected obligation/nemesis card packet within that engine-only
research boundary; public admission, native interaction and integrated wave
gates remain separate work.
