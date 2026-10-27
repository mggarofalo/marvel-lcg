# Hulk Basic reprints independent review

Accepted for the bounded nonpublic packet at
`fc9a01b6186c8aa0e1fcd6aa08d2983dd78a713b`, based on reviewed H1
`43a2e812d7417a12a9f8bc0a17fcc16996ff551b`. This review establishes the five
actual research rows, not complete Hulk content, setup, public admission,
native behavior or whole-game acceptance.

Compared every corrected printed field in `datasets/cards/cards.json` for
`10020`–`10024` with Core `01088`–`01092`: only `card_id` and `pack` differ.
The five authored execution bodies equal their Core counterparts. Energy,
Genius and Strength retain `YY`, `BB` and `RR`, empty ability lists and maximum
one per deck. Mansion and Helicarrier retain their printed exhaust cost,
unrestricted player choice and title-based maximum one per player. Reviewed
RR 1.8 `rr:resource.1` and `rr:max-maximum.2`–`.3`; the local FAQ queried the
four Core/Hulk support IDs and records no entries for them.

Independent parsing of raw UTF-8 JSON object spans confirms all **688**
predecessor objects are byte-identical; exactly `10020`, `10021`, `10022`,
`10023`, `10024` follow, producing **693** rows. Concatenated predecessor
object bytes have SHA-256
`bc6326adc6c28805bbc6676efe827c488095b1ec42ebee4a4205c4aee67375be`.
All sixteen pre-existing Play test changes replace only the total count 688
with 693. Ordered face inventories, counter inventories, bounded Strange suffix
checks and unknown-card controls remain intact. AbilityData adds only the five
explicit permitted faces and still compares the complete authored inventory.

The source tests distinguish both printed resources from one physical discarded
card; Mansion's controller from its physical owner and chosen recipient, both
controller forms, exactly one draw and exhaustion; and Helicarrier's recipient,
price, successful consumption, rejected payment, exhaustion and phase expiry.
Cross-printing title limits reject a Core/Hulk duplicate controlled by one
player, allow another player's copy and recover after departure. The corrected
choice helper answers and finishes the real Sequence continuation; populated
player decks keep the resource-discard assertions from observing deck recycling.
No engine defect was established, and the obsolete external probe is not evidence.

Added `HulkBasicReprintReviewTests` to close the actual-row ordinary-card payment
gap. A non-first controller, physically owning neither source nor necessarily
recipient, activates Helicarrier in either form for either player. The recipient
then plays actual Hulk Mansion through `CardPlay`, using Strength plus one
physical single-resource card to pay the reduced three-resource price. Repeated
pricing and underpayment retain digest, RNG state/word count, agenda and the
discount; valid retry enters the recipient's support area, discards exactly the
selected payment cards into that owner's discard and consumes the reduction.
All four cases pass. No runtime or ability-book change was required.

Independent Windows evidence on the source plus this reviewer test:

- Nonincremental Release Content build: zero warnings/errors.
- Reviewer test alone: **4 passed**, zero failures/skips.
- Complete sixteen affected existing classes, H1 identity/Leap/reviewer tests,
  both Basic reprint classes, all AbilityData and rule citations, plus the
  complete CoreLastingEffectCards class: **186 passed**, zero failures/skips.
- `git diff --check` passes.

External logs under `%TEMP%` are `hulk-basic-review-build.log`,
`hulk-basic-review-adversarial.log`, `hulk-basic-review-affected.log`,
`hulk-basic-review-filter.txt` and `hulk-basic-review-audit.log`; the raw audit
script is `hulk-basic-review-audit.py`. Reviewed the implementation's final four
data-mutant logs: drawing two and drawing for the controller fail Mansion's
outcome assertions; reducing by two and omitting exhaustion fail Helicarrier's
price and readiness assertions. These are inspected implementation experiments,
not independently repeated mutations. The earlier invalid lifecycle and failing
baseline mutant runs are excluded, as the implementation record states.

The normal-hook reviewer commit is recorded in the handoff. Broader managed,
platform, native, public setup and complete-game gates remain at the combined
integration boundary.
