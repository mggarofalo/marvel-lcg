# Hulk hand resource prerequisite — MARVEL-853

Base: `e59755cffbb351f661341cf4fa12d5e43c1afab1`; branch
`feat/hulk-hand-resource`. This packet supplies Q3 without authored production
rows, public Hulk admission, setup or scheduler changes. AbilityRunner only
forwards the explicit hand-source eligibility query.

Printed `datasets/cards/cards.json` 10007 says "Spend this card only in hero
form." Its printed resources remain `RRR`. RR 1.8 `rr:resource.1` permits
resource generation by discarding hand cards; `rr:the-golden-rules` makes this
printed limitation override the general permission. `rr:identity` identifies
form from the faceup identity. The vendored FAQ lists 10007 in its queried
coverage but contains no individual Limitless Strength ruling; no historical
ruling is substituted for printed text.

The live trace is `CardPayment.Generators` → `ResourcesGeneratedBy` →
`AbilityProgramQueries.ResourcesGeneratedBy`. `CardResourcePayment.Validate`
and `Contribute` query the same capability. Card-level `handResourceForm:
"hero"` metadata restricts that payment query by the source owner's live form,
including null payingFor (ability cost) and printed card prices. Strict JSON
admission rejects malformed or unsupported values; lowering rejects unsupported
forms and unauthored faces. `Resources.PrintedCount` continues to read printed
facts directly. A triggered Resource ability is neither needed nor granted.
The metadata spelling and its current hero-only vocabulary are engine choices.

Synthetic tests label their boundaries. They cover Hulk/Banner forms, an
otherwise valid same-cost unrestricted control, form-switch stale payment,
rejection preserving digest/RNG/outstanding agenda, successful retry, printed
card price and ability cost, absent Resource abilities, printed icon inspection
and synthetic in-play text blanking. This is engine evidence, not public pack
acceptance or native UI evidence.

External logs use `C:/Users/mggar/hulk-hand-resource-*.log`. Restored focused
results and compiled mutant results are recorded by the implementation packet;
independent review and combined integration certification remain separate.

Verification: affected restored run passed **47 tests** after a forced
`--no-incremental` build (zero warnings/errors). Three compiled mutants were
killed: removing live eligibility (3 failures / 2 passes), bypassing programmatic
admission (2 failures), and accepting arbitrary JSON form strings (3 failures /
4 passes). The restored affected filter includes HandResourceForm,
HandResourceCommitment, ResourceIconDiscard, PhysicalPrintedPrice,
AttackEventResource and ThorResourceCard. Normal hooks run on the packet commit.

## Individual contributor validation repair

Independent review `acb926811d93d92efe420512ce34fcade46aea28` established
that returning no resources for an ineligible source did not reject that source
when other contributors covered the price, including zero. Its regression and
review record are cherry-picked as `67f6d66af7bd8979edbbda97fd280a22e9c6bd51`.

`IResourceCardAbilities.CanUseHandResources` now exposes eligibility separately
from generated quantity. The immutable program query owns the form check;
AbilityRunner forwards it and NoCardAbilities permits sources without printed
restrictions. Engine offers use that predicate, and `CardResourcePayment` applies
it while validating every selected hand card before any commitment and again at
that contributor's actual commitment, including after generator suspension.
Aggregate sufficiency cannot authorize a restricted source. The existing payment
entry contracts and serialized receipts remain unchanged.

The existing unrestricted overpayment and zero-cost controls remain legal. A
synthetic zero-icon hand source demonstrates that empty generated quantity is
not itself classified as forbidden. A synthetic generator suspension establishes
that a form change prevents the later uncommitted restricted hand discard while
preserving the already committed generator cost; no rollback promise is made.
A same-form continuation completes normally.

Repair evidence is outside the repository under
`C:/Users/mggar/hulk-hand-resource-repair-*.log`. Eight focused reviewer/repair
controls pass. A compiled mutant removing individual eligibility from the shared
hand-source check is killed by the stale zero-cost, stale overpayment and resumed
form-change controls (3 failures / 5 passes). Restored sources receive a forced
whole-solution `--no-incremental` build before affected payment verification.
Independent review closure remains separate.

Restored repair verification: whole-solution Release build passed with zero
warnings/errors; **141 affected Content tests passed** after that build. The
filter covers all HandResourceForm, HandResourceCommitment, ResourceIconDiscard,
PhysicalPrintedPrice, AttackEventResource, ThorResourceCard,
ResourcePaymentContinuation, RestrictedResource, VariableEventPayment,
EventRestrictedResource, MsMarvelPayment and PreparationResource classes.
