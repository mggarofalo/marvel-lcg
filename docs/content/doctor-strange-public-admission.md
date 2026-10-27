# Doctor Strange public admission evidence

MARVEL-837. Candidate based on `9d9eb7e7b6e517d677b5776d9064d185c23ef962`.
This commit changes complete-product admission, authored setup and their tests;
it does not alter the ability book, engine resolution or client UI.

The mc08 divider pin in `datasets/rules-packs/mc08/UPSTREAM.md` supplies the
40-card starter (15 signature, 17 Protection, 8 Basic), separate identity,
obligation, five nemesis cards and five Invocations. Admission requires exactly
09001a/b and 09002-09039: forty unique printed faces. The authored Invocation
recipe is separate from the ordinary deck and pins 09032-09036 once each in
allocation order. Extra customization cards 09037/09038/09039 are absent from
the starter and enter public custom decks at limits 3/3/1. Product quantities,
not divider packaging or marketing totals, govern the gate.

Public discovery has twelve heroes, 39 scenario/mode recipes and fourteen
encounter sets. Tests use the normal DatasetGameFactory and EngineHost paths:
Core and Rise of Red Skull Standard/Expert setup at one through four seats,
Doctor Strange in non-first seats, every mulligan before public top exposure,
separate owned piles, legal Protection substitutions, all three extra cards,
Invocation exclusion from ordinary recipes, and a naturally shuffled Winds of
Watoomb resolved through Spell Mastery after a public form change. Embedded
and socket deck catalog tests cover the new starter/extras and preserve Hulk
exclusion. Unsupported-product fixtures continue rejecting authored Hulk cards,
recipes, deck quantities and game creation rather than using the admitted pack.

The setup authority record is explicitly outside-Core. Behavioral catalog
counts are 2,668 sources and 4,435 obligations; setup contributes 65 sources.
This does not claim new executable Core transcripts.

Verification used Release on Windows, with logs/TRX outside the repository at
`C:/Users/mggar/Source/strange-admission-evidence/`:

- `content-setup-namespace`: 1282 passed, 0 failed, 1282 total.
- `server-restored`: 79 passed, 0 failed, 79 total.
- `rules-final`: 25 passed, 0 failed, 25 total.
- `mutant-invocation`: 0 passed, 2 failed, 2 total.
- `mutant-starter`: 7 passed, 1 failed, 8 total.
- `restored-admission`: 12 passed, 0 failed, 12 total.

`content-setup-namespace` selects the whole `Marvel.Content.Tests.Setup.`
namespace. `server-restored` selects whole affected public setup/action/custom
classes, DeckCatalogTransport, RuntimeProductAdmission, StandaloneProductDiscovery,
EngineHostAuthoredSetupChoices, DeckAssessment and existing Ms. Marvel/Thor/Black
Widow public classes. `rules-final` selects whole BehaviorAuthorityTests and
ExpansionSetupAuthorityTests. Offline Behavior.Index check and Cards.Extract
check both pass without network acquisition.

The compiled Invocation mutant removes both separate-recipe guards and is killed
by missing/reordered Invocation admission tests. The compiled starter mutant
removes the exact customization-quantity guard and is killed by a changed but
otherwise legal forty-card starter. Both use the real admission path. Source is
restored in finally blocks, followed by a successful nonincremental Content test
project build and the whole admission class plus published-recipe test.

Earlier wider `FullyQualifiedName~Setup` evidence is partial/failing: 1,313
passed, two failed, 1,315 total. One failure is the accidentally selected Core
transcript `card-actions.feature:331` (expected a pending action prompt after
stun cancellation); the lead separately reproduced it and owns its closure.
The other is a precompiled negative fixture that supplied a fourth Momentum
Shift; the final fixture instead substitutes a third Med Team, asserts ordinary
deck legality, and passes. Two still-earlier outdated runs were stopped after
stale hero counts/exclusion examples were discovered; a concurrent Content
rebuild failed with MSB3027/MSB3021 because that outdated test process held its
DLL. Those runs provide no final green evidence. All subsequent checks are
serialized. Original non-ASCII characters in edited existing files are preserved.

This packet establishes focused public admission/setup/action evidence only.
Representative complete games, fresh-host/save/replay journeys and final combined
pack/platform/package acceptance remain separate MARVEL-838 work. No broad native
or full Godot lane, packaging CI or Linux certification ran in this packet.

## Independent admission review

Reviewed source revision `d887173f0eed622cbabbcd6a466d7744bdccc75d` on
`review/strange-public-admission`. No blocking admission finding remains.
The review owns tests and this evidence addition; it changes no engine,
ability-book, dataset, schema, client or transport implementation.

The local mc08 divider was independently inspected and its SHA-256 matches
`81a2470bfb508bc0971b1e1289ccb7c7a1368fcb04d9d689575cdf47b93fa402`.
Its starter is exactly 15 signature, 17 Protection and 8 Basic cards; identity,
09027 obligation, 09028/09029/09030/09030/09031 nemesis and the five distinct
09032-09036 Invocations are separate. The complete inventory gate requires
09001a/b and 09002-09039, including the three customization faces. All 685
authored faces reconcile by product, including exactly 40 Doctor Strange faces.
The added authority source and adjudication pin this setup and remain outside
Core. Changed global inventories consistently report twelve heroes, 39 scenario
recipes and fourteen encounter sets. Hulk remains unsupported across public
recipes, authored research rows, custom decks, quantity maps and game creation.
A byte-based audit of all 39 changed source paths found no lost or altered
existing non-ASCII lines; every changed source file decodes as UTF-8.

`PublicDoctorStrangeAdmissionIndependentTests` adds nine production-path probes:
four-seat custom setup with Doctor Strange in each seat, checking ordinary deck
contents, individual Invocation ownership and projected deck/discard ownership;
and all five Invocations substituted into otherwise legal forty-card starters.
The latter check the specific unsupported-classification diagnostic, zero
permitted quantity, factory rejection, host rejection and an empty session store.
The unchanged legal starter supplies a positive control, so ordinary deck size
cannot account for rejection. No research-only catalog or direct world bypass
is used for these public admission claims.

Independent Windows Release evidence is in
`C:/Users/mggar/Source/strange-admission-review-evidence/`:

- `content-setup.trx`: 1,282 passed; exact Content Setup namespace.
- `server-review.trx`: 88 passed; affected public setup/action/custom-deck,
  inventory, assessment and both embedded/socket catalog classes, including the
  nine independent cases.
- `mutant-hero-class.trx`: five intended failures. The compiled mutant permits
  Hero-class customization cards; every forty-card Invocation substitution fails
  at the rejection assertion, distinguishing this guard from size rejection.
- `restored-independent.trx`: nine passed after restoring original source bytes
  and a successful nonincremental Server test-project build.
- `rules-review.trx`: 25 passed; complete BehaviorAuthorityTests and
  ExpansionSetupAuthorityTests.
- Offline Behavior.Index and Cards.Extract checks pass.

The initial Content and Server baseline processes briefly overlapped; mutation,
restoration and subsequent checks were serialized. The old Core transcript
pending-target finding is separate lead-owned work, not waived by this verdict.
This review approves focused public admission, recipe composition and setup
ownership. Catalog transport coverage does not establish a complete client
journey. No schema migration, fresh-host save/restart/replay journey, complete
game, Godot interaction acceptance, Linux or package certification is claimed;
those remain the MARVEL-838 integration scope.
