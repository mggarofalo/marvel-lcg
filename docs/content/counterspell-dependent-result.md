# Counterspell dependent cancellation result

Source: `24285d394b0988ea8a4bff1059366a90c5957d85`; branch
`feat/counterspell-result`. This is a Cards prerequisite with test-local
09030 compositions. Production remains 682 rows / 37 Strange faces. No
ability-book, generated authority, public admission, Runner or Agenda edits.

RR 1.8 `initiating-abilities.step.5-.7` pays costs before initiation and
resolves or cancels the played event before normal discard. `event.2` says
“Only the effects are canceled.” `then.1-.2` requires full predecessor
completion. `and.1-.2` keeps cancellation and event discard independent.
The full printed candidate remains `cancel its effects and discard it.
Then, discard this card`; its independent AND requires the existing first
player ordering decision, explicitly answered in the candidate tests.

Admission reads an outstanding occurrence by reference, or the existing
id-zero prospective paid-event candidate with matching physical subject,
player ownership, event kind and revealing area. The prospective candidate
never cancels anything. Execution returns full only when it actually removes
that exact outstanding occurrence; already completed or copied occurrences
return none. Existing structural aggregation combines committed child
results: a repeated cancellation in synthetic sequential pre-Then text is
partial and cannot discard the attachment. Full cancellation plus explicit
event discard permits the printed Then. Costs remain paid and event discard
precedes attachment discard, exactly once.

Status priority is settled after accepted event and arrow costs, before the
event's competing triggered interrupts. It uses explicit ability labels or
a single Power wrapped only by ChooseCard nodes. It does not scan branches,
other abilities or arbitrary nested powers. The existing labeled status
owner removes stun/confusion. An inactive attack branch control preserves
stun while Counterspell cancels the selected nonattack event.

Evidence is external under `C:/Users/mggar/Source/counterspell-*.log`:
`expanded-fixed` passes 37 cases across complete candidate, result,
event cancellation, modifier lifecycle, ability-data and citation classes.
The final restored selection additionally includes the inactive-branch test.
`mutant` compiles cancellation falsely succeeding for a completed occurrence;
its repeated-cancellation and exact-occurrence controls must fail. `restored-build`
forces a nonincremental Release rebuild before `restored-tests`. `architecture`
and `commit` capture the layout gates and ordinary pre-commit hooks.

The candidate checks attachment in both forms, alter-ego reveal whiff, another
seat, status priority, Invocation Special exclusion and Master event play.
Additional controls cover two legally attached Counterspells, one event discard,
paid resources, stale and copied occurrence identity, reusing the same physical
event, and query baselines (digest, RNG, pending work, effects, occurrence and
resolution entries) with subsequent execution compared to an unprobed board.
No transient receipt, RNG stream, digest, save or protocol format is added.

Limits: generic conditional/nested status powers retain their existing timing
boundary; this repair admits only the bounded label/target-choice envelope.
Public pack admission, save/restart, native interaction and whole-pack
certification remain outside this prerequisite.

Lead closeout extracts paid-event initiation and the event-before-attachment
discard assertion into cohesive helpers, preserving the behavior and assertions.
The resulting selection passes 63 cases across Counterspell, event cancellation,
played-event modifiers, citations and the actual `~AbilityData` class prefix.
Earlier `~AbilityDataTests` filters do not match the split concrete classes and
are not evidence that those dataset gates ran. The closeout log is
`counterspell-lead-closeout-tests.log`; normal hooks remain enabled.

## Independent result review

Reviewed source `e49fdf23` in `review/counterspell-result`, using the
marvel-pack-delivery review workflow. Verdict: accept the bounded Cards
prerequisite and full test-local printed Counterspell composition. This is
not admission of Doctor Strange or certification of generic conditional or
arbitrarily nested status powers. Explicit selected ability labels apply to
their whole body; an unlabeled nested power does not establish a whole-event
label by scanning the tree. The current priority repair unwraps only
ChooseCard around one direct power.

Authority review used the generated 09030 text, RR 1.8 initiating abilities
steps 5–7, event.2, then.1–2, and.1–2, status-cards.2 and labeled-ability.6,
plus local ruling `4432bdf7194d532c`. Its older 1.6 status-priority answer
agrees with current RR 1.8. Existing candidate controls cover attached seat,
both forms, Magic Blast under stun, Master of the Mystic Arts cancellation
and Invocation Special exclusion. Exact-reference cancellation returns none
for copied, completed or stale nonzero occurrences, including reuse of the
same physical event. The id-zero candidate is admission-only.

`CounterspellIndependentReviewTests` adds 13 cases: matching prospective
occurrences and copies; wrong owner, area, card kind, condition, id or subject;
both first-player copy choices crossed with both independent AND orders;
and selected dual labels versus a same-cost body whose labels belong only to
another ability. Both copies are legally attached by the reveal procedure.
The winning copy and physical event each discard once, event first. Paid
Momentum Shift healing remains paid while the attack deals no damage.
Explicit attack/thwart labels consume both statuses after the exhaust cost;
the unselected ability leaves both statuses intact while Counterspell resolves.
Neither cancelled draw reaches the prepared top card.

External evidence is under `C:/Users/mggar/Source/counterspell-review-*`:

- `baseline.log`: 80 selected pre-review Content cases passed.
- `new-fixed.log`: all 13 added cases passed. Initial probe failures were
  fixture corrections: 09007 is an Event (09010 supplies the Upgrade control),
  and Momentum Shift's two healing is a paid cost.
- `owner-mutant.log`: removing prospective ownership matching compiled and
  failed only the wrong-owner case (1 failed / 12 passed).
- `label-mutant.log`: omitting explicit labels compiled and failed only the
  labeled body (1 failed / 12 passed), which incorrectly reached Counterspell's
  ordering prompt instead of status cancellation.
- `restored-build.log`: restored sources rebuilt with `--no-incremental`,
  Release, zero warnings/errors.
- `restored-tests.log` and `evidence/counterspell-review-restored.trx`:
  341 passed, zero failed/skipped, after restoration. The whole-class selection
  uses `~Counterspell`, `~EventPlay`, `~EventModifier`, `~AbilityData`,
  `~RuleCitation`, `~CardPowerContinuation`, `~PowerContinuationDependency`,
  `~ActionAbilityContinuationBindings`, `~ActionAbilityRepeatedEffects`,
  `~ActionAbilityRepeatedTrace`, and `~ActionAbilityPaymentsAndAuthorization`.

The review changes only tests and this record; temporary production mutants
are restored. Normal commit hooks provide the architecture/layout, complexity
and file-length checks. No full suite, public setup, save/restart or native UI
claim is made by this review.
