# Doctor Strange Invocation rows — MARVEL-813

Base `1edebb22`; branch `feat/strange-invocation-rows`, 2026-10-26.
This bounded content packet authors **Images of Ikonn 09033** and **Vapors of
Valtorr 09035** on nonpublic boards. The book has 663 rows and 18 of Strange's
40 printed faces. Every one of the predecessor 661 raw JSON row objects is
identical, checked independently of semantic JSON equality. No engine, UI,
public setup, product admission or printed dataset changes are included.

Printed authority is `datasets/cards/cards.json`. Images follows confuse the
villain, choose a scheme/remove four, and separate physical self-discard. Vapors
uses the independently reviewed physical-status replacement primitive and its
three legal different-kind destinations. Invocation costs belong to the calling
Spell Mastery or Master arrow, rather than ordinary Event play. Current RR1.8
any-part targeting and the audited clarifications `8c9c9d30f4c01189` and
`8883bc6b0af1c9b8` preserve independent self-discard when the main effects are
ineffective. The reviewed Master ruling `b07d53e12625ab23` requires intermediate
discard/reveal/reset before returning the original physical card.

The existing Images choice and Vapors characteristic suites now load actual
production rows without replacing them. The simplified direct-main-scheme
Images admission control, synthetic paid Crimson and other prerequisite controls
remain explicitly synthetic; the replacement helper still removes the exact
existing face before adding a synthetic row, and duplicate admission is unchanged.
Actual coverage includes non-first ownership, another player's exact status,
illegal and stale answers preserving state/RNG/pending work, chosen-scheme
suspension, same-kind/duplicate/Stalwart filters, no-status/no-threat legal
self-discard, ordinary and last-card decks, conservation and exact Master return.
The added Vapors matrix verifies exact intermediate reveal, shuffle occurrence
and RNG consumption on both arrows. DSL documentation now records the admitted
status query/replacement vocabulary. Inventories permit only these two new faces.

Crimson remains unauthored behind **MARVEL-814**
(`1bbd2461-61a3-498e-91ff-748a9e8c2863`). A proposed genuine enemy choice containing
stun then seven non-attack damage passes the four guard/non-attack/lifecycle
cases, but fails independent self-discard when its sole enemy is already stunned
and cannot take damage. The minimized synthetic prohibition uses an existing
`preventDamageWhile` constant on the fixture villain; it is not printed Rhino
behavior. Both unprotected same-cost controls pass. Both protected cases skip
the Special: Spell Mastery leaves the original top; Master emits Return from
AdditionalDeck without discard/reveal. No engine workaround is included.
Seven Rings 09034 remains separately unauthored; no sequential recipient proxy
is introduced.

Exact external reproduction artifacts under `C:/Users/mggar/Source/`:

- `invocation-rows-crimson-candidate-book.json`: full candidate book with the
  proposed 09032/09033/09035 programs and unchanged predecessor objects.
- `invocation-rows-CrimsonIndependentDiscardProbeTests.cs`: exact four-case
  failing probe/control source. Restore the candidate book and copy this source
  under Content Play; the committed fixture retains `BoardActualWithRunner`.
- `invocation-rows-DoctorStrangeInvocationCardTests.cs`: initial candidate's
  four Crimson guard/non-attack and last-card lifecycle cases plus Vapors cases.
- `invocation-rows-crimson-probe.log`: two missing-self-discard failures and two
  unprotected passes, after correcting an initial unsupported sourceKind fixture.

Verification logs in that same external directory:

- `invocation-rows-images-mutant.log`: the authored amount 4 changed to 3 lowers
  and runs; four positive scheme-choice cases fail (expected two threat, actual
  three); six independent admission controls pass.
- `invocation-rows-vapors-mutant.log`: tough replacement changed to stunned
  lowers and runs; four cases fail at the missing legal tough destination and
  blocked-card filtering; four controls pass.
- `invocation-rows-restored-build.log`: restored Content Release build with
  `--no-incremental`, zero warnings/errors.
- `invocation-rows-restored-focused.log`: all 186 affected cases pass after
  restoration, covering whole Strange, Invocation, Master, Spell Mastery,
  Vapors, StatusReplacement, AbilityData and RuleCitation classes. Final exact
  reveal assertions receive the final focused rerun below.

Independent content review and integrated broader delivery checks remain with
the lead. This packet establishes neither a whole-pack completion nor a public
or native product journey.

Final checks after exact-reveal assertion additions:

- `invocation-rows-final-focused.log`: 186/186 affected Content cases pass.
- `invocation-rows-architecture.log`: whole Architecture 129/129 passes; the
  final additions only strengthen test assertions and correct its inventory name.
- Normal commit hooks (architecture policy, complexity and file length) run
  without bypass. Their output is retained in `invocation-rows-commit.log`.

Lead closeout extracts the exact-reveal assertion into its own helper after the
normal complexity hook found CCN11 in the lifecycle test. All assertions remain
unchanged. The final rebuilt selection passes all 186 cases; evidence is
`Source/evidence/invocation-rows-root-final.log`. The original hook failure is
retained in `invocation-rows-commit.log`; the normal retry is recorded in
`Source/evidence/invocation-rows-root-commit.log`. No gate is bypassed.

## Independent two-row review

Reviewed candidate `06fcca2a` against `1edebb22` on 2026-10-26 in
`review/images-vapors`. **Verdict: accepted for this bounded nonpublic content
packet; no blocking correctness finding.** This does not accept the complete
Doctor Strange pack, public admission, or a native product journey.

The review read the generated printed 09001a/09005/09033/09035 facts, current
RR 1.8 target/choose/status/Stalwart clauses, and the audited clarification graph.
The older 1.5 no-main-effect answers remain consistent with current any-part
targeting; the audited 1.6 Master clarification requires discard, intermediate
reveal or recycle, then return of the original physical copy. The two JSON
programs follow those instructions. A separate raw JSON object-span comparison
confirmed all 661 predecessor objects unchanged, with exactly 09033 and 09035
appended (663 rows and 18 authored Strange faces).

The whole relevant fixture classes were reviewed for actual-row loading,
meaningful starting states, non-first answering ownership, costs, suspended
choices, status host identity, invalid/stale answers, physical conservation and
completion. Actual Images and Vapors now use `AuthoredCards.Runner` directly.
The explicit replacement helper still removes only the exact existing face;
parser duplicate rejection is unchanged. Direct-main-scheme Images, paid
Crimson and six-direction primitive controls remain declared synthetic.
Captured source ownership/incarnation, live payment suspension, and query
purity controls remain in the reviewed base; these are engine prerequisite
evidence rather than new actual-row claims. The existing row mutants distinguish
three versus four threat and a missing tough destination. Their failure logs and
the rebuilt restored 186-case result were inspected, along with the original
complexity failure and successful normal-hook retry.

`InvocationRowsIndependentReviewTests` adds 12 actual Vapors cases exercising
every directed different-kind replacement through both calling arrows. Each
checks both available destinations, the selected physical status and host,
replacement events, untouched other status, self-discard, return/top ownership,
RNG and completion. Two actual Images cases distinguish its printed one-cost
Special from Master's combined two-cost payment, reject empty payment without
changing prompt/state/RNG/pending work, and successfully retry the same offer.
This closes the production-row destination coverage gap left by the synthetic
primitive matrix; the reviewed code already implements the correct behavior.

Independent Release verification selected whole DoctorStrange, Invocation,
MasterOfMysticArts, SpellMastery, Vapors, StatusReplacement, AbilityData and
RuleCitation classes: **200 passed, zero failed/skipped**. The log is
`C:/Users/mggar/Source/evidence/images-vapors-independent-content.log`.
Normal review-commit hooks are retained at
`C:/Users/mggar/Source/evidence/images-vapors-independent-commit.log`.

Crimson 09032 remains unauthored behind MARVEL-814: the external genuine
enemy-choice candidate's two protected cases miss independent self-discard,
while both same-cost unprotected controls pass. That evidence was inspected,
not represented as a passing authored row. Seven Rings 09034 remains a separate
unauthored capability. No book, engine, printed-data, setup or UI changes are
made by this review. Integrated broad checks remain the lead's responsibility.
