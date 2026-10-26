# Ms. Marvel public admission and restart review

Reviewed candidate: `5034a368`, in a new isolated worktree. This packet changes
only independent tests and this review record. It applies the pack-delivery
implementation/proof guidance and does not establish native or whole-game acceptance.

The pinned `datasets/rules-packs/mc05/ms-marvel-deck-divider.jpg` was visually
inspected. Its SHA-256 is
`0549958a91b4ce7d258055e5f04e37777768db9df9b33af1e7a0be8b7f2c1798`.
The printed quantities agree with the raw `msm.json` and `msm_encounter.json`
snapshot and authored recipe: 15 signature cards, 14 Protection cards, 11 Basic
cards, Home by Dawn, and five nemesis cards including two Harvest. Both identity
faces belong to one physical identity. The extra pool 05030–05033 is separate
from the starter. The mc05 rulesheet is descriptive and adds no executable rules.

`MsMarvelRecipe` pins each composition, the identity pair, nemesis icon and
ordinary aspect construction. `StandaloneProductAdmission` checks the precise
34-face catalog inventory and authored membership. The shared runtime gate then
controls setup, executable cards, deck discovery/assessment and game creation.
There is no test-only bypass in the public journey.

The independent coverage adds:

- Each of 34 required authored faces removed individually: runtime closes the
  entire pack while retaining eight other heroes, Wrecking Crew, Green Goblin
  and the Rise of Red Skull campaign.
- Each printed catalog row removed, and each row's product metadata changed,
  individually: the complete inventory predicate rejects admission even when
  that face remains authored.
- Every extra-pool card in a legal public custom deck, using the corresponding
  aspect's admitted starter extras; exact dealt contents are checked. Public
  discovery has precisely nine reviewed heroes and still rejects Thor.
- Normal starter seed 3, keep, Teen Spirit, flip and Big Hands, followed by a
  serialized pending Morphogenetics prefix cloned into a separate store and a
  freshly loaded production factory. Teen Spirit must add a signature face.
  Accept and decline are compared separately with uninterrupted controls:
  complete saves/journals and events agree, the exact played event returns only
  on acceptance, exhaustion is paid only on acceptance, and four damage remains.

The candidate's existing recipe-negative controls include a legal 40-card deck
with wrong published quantities. Existing production setup covers one through
four players and both Core/Rise of Red Skull difficulties. Its original restart
test also proves unchanged pending prompt/save, exact event recovery and matching
serialized continuation. The new independent stores strengthen that evidence.

Verification logs are outside the repository under `C:/Users/mggar/Source/`:

- `ms-admission-independent-content.log`: 268 passed, including independent
  admission, published recipe, standalone admission, global setup inventory and
  expansion setup authority checks.
- `ms-admission-independent-server-verified.log`: final public Ms. Marvel,
  adjacent-product, deck transport/assessment and public Green Goblin regression
  filter; result recorded in the review handoff.

No production correctness finding was identified in this admission/restart scope.
The authored ability behaviors, full games, actual rendered UI and atomic
filesystem persistence require their own evidence. No production mutant was
introduced by this test-only review; the implementer's recipe mutant evidence
is separate from the independent checks above.
