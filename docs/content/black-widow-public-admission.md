# Black Widow public admission packet

MARVEL-789. Source base: `3684923b`; this packet changes setup policy, tests
and supporting scope metadata only. The 645-row ability book and setup JSON
are unchanged. This is candidate public integration, not full-pack acceptance.

The policy requires the exact 34-face bkw catalog (08001a/b and 08002–08033),
every authored face and the published recipe together. The recipe pins the
40-card starter (15 signature, 17 Justice, 8 Basic), one Burn Notice and five
nemesis cards. Source author visually checked the pinned divider JPG against
mc07/UPSTREAM.md. Independent review of `e6f3bba72cdafe53d6c49f1e2d9f4c556a0ac87d`
also read the raw image and generated card facts, as recorded below.

Behavioral controls remove each required authored/catalog face, change each
face's product membership, substitute an unreviewed face at equal inventory
size, and change every required recipe component. The wrong starter control
replaces one Stealth Strike with Interrogation Room: it remains a legal 40-card
deck, independently admitted before the complete-product policy rejects it.

Public checks cover Standard/Expert Core and RoRS setup at one through four
seats, printed starter and encounter composition, all four extra-pool cards,
fourth-seat custom-deck ownership and rejection without durable publication.
Both transports pin discovery membership and zero deck quantities for both
identity faces. Doctor Strange replaces stale unsupported Black Widow examples;
all their rejection assertions remain. Natasha's response after playing a
naturally dealt, legally paid Target Acquired is saved through strict stored
session JSON, reconstructed by a fresh factory/host, accepted or declined and
compared with uninterrupted play for prompt, hand, events and complete save.

Evidence lives outside the repository under C:/Users/mggar/Source/:

- widow-public-content.log: initial broad Setup filter, 1154 passed / 2 failed
  of 1156. A stale GreenGoblinAdmissionTests unsupported bkw example is corrected
  to drs. Existing CoreRuleTranscriptDefeatBranchesHavePinnedOutcomesTests.
  SetupBranchesHavePinnedOutcomes fails at Rhino 01103, expecting damage while
  an interrupt to WhenDamageWouldBeDealt is pending. Root owns that shared
  engine regression; its assertions remain unchanged.
- widow-public-server.log: initial 33 passed / 4 failed; the new restart fixture
  omitted Target Acquired's printed one-resource payment, and discovery expected
  the new hero in the wrong position. Both corrected.
- widow-public-server-restored.log: 58 passed / 3 failed of 61. Both restart
  branches pass. The three stale compiled discovery expectations are corrected
  to canonical setup order: spider_woman, black_widow, thor.
- widow-public-recipe-mutant.log: compiled deletion of the player-deck quantity
  check fails exactly the legal wrong-starter case (1 failed / 8 passed).
- widow-public-restored-build.log: source restored, forced non-incremental build
  passes with zero errors.
- widow-public-content-restored.log: final namespace Setup plus expansion and
  behavioral authority run, 1122 passed, zero failed.
- widow-public-server-final.log: final public host group, 61 passed, zero failed.

The final checks run serially, without changing authority catalogs or gameplay
sources. Broader content regression, complete games, native interactions and
platform certification remain outside this packet.

## Independent admission review

Reviewed source: `e6f3bba72cdafe53d6c49f1e2d9f4c556a0ac87d`, against `3684923b`.
No production correctness finding in this admission diff. The reviewer visually
read `datasets/rules-packs/mc07/black-widow-deck-divider.jpg` and independently
confirmed its SHA-256 as
`47b74c8eb47614e6a247f273e88eb15217f884138f7082519430a31aeffcc728`.
An independent literal transcription compared every printed title, canonical ID
and quantity with the generated catalog and setup JSON: 15 signature, 17 Justice,
8 Basic; identity separate; 08025 once; 08026/08027/08029 once each and 08028 twice;
08030–08033 absent from the starter. Actual ability data has 645 rows and all
34 printed Black Widow faces.

The reviewer inspected the complete-face/membership negatives, legal wrong-starter
control, compiled recipe-gate mutant failure and completed restored logs rather
than inferring passes from pending runs. Adjacent Doctor Strange rejection,
quantity-map absence and retained supported-product assertions remain intact.
The public restart slice uses the ordinary dataset factory, legal payment and
strict JSON round-trip; no research world or catalog bypass establishes it.

Additional review controls test both identity faces and a foreign signature as
removal-only imported cards, required-signature exclusion from editor quantities,
editable extra-pool quantities, legal repaired construction and missing-signature
rejection. The restart branches also attempt stale and unknown-offer answers after
fresh-host reconstruction and assert unchanged durable save, revision and prompt
before continuing the accepted/declined response against uninterrupted play.
Focused result: 3 passed, zero failed (both restart branches and quantity control),
recorded in `widow-public-independent-review.log`.

Verdict is confined to public admission and the tested response restart boundary;
it does not establish complete-pack, native UI or platform acceptance. The known
shared Core transcript failure remains separately owned and unchanged.
