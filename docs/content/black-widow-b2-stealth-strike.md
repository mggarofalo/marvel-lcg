# Black Widow B2: retained Stealth Strike defeat

Base: `f601932b`, 2026-10-26. This is a nonpublic engine prerequisite,
not production authoring or Black Widow admission. Printed authority is
`datasets/cards/cards.json`, face `08013` (Stealth Strike). Its printed
three-resource Hero Action deals four attack damage; the threat removal
requires that this attack actually defeated its enemy.

## Implementation

`AbilityDamageCompletion.Defeats` retains settled attack-occurrence defeat
tuples for ordinary attacks as well as staged attacks. The tuple identifies
the physical card and its defeated incarnation. Recipient snapshots serve
staged attack aftermath and are not a requirement for retaining an ordinary
attack's actual defeat result.

The test-local full printed composition chooses the enemy, resolves one labeled
attack, and then checks `wasDefeatedByDamage(chosen)` before choosing a scheme.
This retains the causal enemy binding while permitting either current eligible
scheme. The scheme choice belongs after the complete attack. The attack body
contains only its damage instruction. No suspension guard, projection traversal,
overkill ordering, ability-book row or public admission changes.

The initial trial with this outer composition failed both ordinary lethal
controls because the ordinary attack receipt was absent. Moving the scheme
choice inside the labeled attack remains explicitly unsupported, verified
before payment with unchanged state/RNG/pending work.

## Evidence

`StealthStrikeContinuationTests` provides eleven cases:

- Ordinary lethal minion and Hydra Soldier defeat both offer threat removal;
  surviving Madame Hydra and actual authored Loki healing offer none.
- Both main and side schemes can be selected, directly and after a synthetic
  defeat response suspends for its own option choice. Threat is unchanged
  throughout that response. A synthetic reentry during the response preserves
  the original defeated incarnation without attributing defeat to the new copy.
- The scheme continuation retains the exact defeat tuple; a scheme still in
  the encounter deck is rejected without changing the card digest, RNG state,
  pending work or receipt, and the valid answer succeeds on retry.
- Zero threat finishes without a scheme prompt. Repeated direct choice queries
  preserve state/RNG/pending work and produce the same final state and event
  trace as an otherwise identical unprobed resolution.
- An arbitrary scheme choice inside the labeled attack still fails before costs.

The compiled distinguishing mutant substitutes four damage taken for actual
defeat. Both the surviving Madame Hydra and healed Loki cases fail by offering
unearned threat removal (two failed, seven passed). The production source was
restored and rebuilt with `--no-incremental`; the restored regression selection
passed 168 tests. Two additional final tests then passed with all eleven local
Stealth Strike cases. The final combined selection is recorded at handoff.

External logs are under `C:/Users/mggar/AppData/Local/Temp/bw-stealth-*`:
`initial.log`, `receipt.log`, `expanded.log`, `arithmetic-mutant.log`,
`restored-build.log`, `restored-focused.log`, and `final-local.log`.
The regression selection includes `PowerTrace`, `PowerContinuationDependency`,
`ContinuationBindings`, `LabelledPowerDefeatContinuation`, `LokiRr18Overkill`,
and `Preparation`, alongside Stealth Strike.

## Remaining proof

Integrated broader gates remain required. No public Black Widow setup journey
is claimed.

## Independent review and durable evidence

Independent source/authority review of `527b6aedb9a23627aa50b4027e3f9b3069a64401`
found no correctness defect in this bounded engine prerequisite. The printed
08013 text requires actual defeat by this attack; RR 1.8 damage steps 6–9 and
the current defeat definition govern. The historical Loki answer's conclusion
for Stealth Strike agrees with this requirement; its older overkill explanation
does not govern RR 1.8. The implementation reads the owned attack execution's
settled occurrence, and the condition compares the retained card/incarnation.
The only production `wasDefeatedByDamage` composition in the reviewed ability
book is 07016's direct damage; this patch does not alter that path.

`StealthStrikeAttributionReviewTests` adds three independent controls. A stunned
attack pays and discards its event/payment but damages no enemy and removes no
threat. A synthetic nested response's direct damage may defeat either the
attacked survivor or a different enemy; neither defeat earns Stealth Strike's
threat removal. Together with the original eleven cases, these challenge
unrelated causal attribution, cancellation, Loki replacement, reentry, no threat,
query purity and invalid-answer retry.

`StealthStrikeReplayReviewFactory` uses the existing supported
`IDurableGameFactory` injection seam, with a test-only full printed 08013
composition and a synthetic 08023 defeat-response choice. Its two main/side
cases strictly round-trip session and stored-session JSON, replay the complete
prefix, and instantiate fresh `EngineHost` objects from copied stores at the
suspended response and again at the following scheme choice. They compare
uninterrupted/replayed/restarted state digest, full RNG state and consumed-word
count, prompt records, phase, round and subsequent raw event traces. A foreign
scheme answer preserves the canonical save and live state/prompt/RNG before a
valid retry. Both paths finish the action with the expected two threat removed.
This proves reconstruction through the host's startup path, not a separate
process or filesystem durability test. No production row or admission changes
are included.

Review validation on Windows:

- All fourteen Stealth Strike content cases passed.
- Both new durable cases passed; the combined restored server selection also
  includes the existing Preparation replay review.
- A compiled regression mutant restores the staged-recipient filter in
  `AbilityDamageCompletion.Defeats`; both durable cases fail because the scheme
  continuation disappears. The source was restored byte-for-byte and rebuilt
  with `--no-incremental` before the restored verification.

Logs are outside the repository under
`C:/Users/mggar/AppData/Local/Temp/bw-stealth-review-*`: `content.log`,
`server.log`, `mutant.log`, `restored-build.log`, `restored-server.log` and
`restored-content.log`. Ordinary pre-commit gates remain enabled for the review
commit. Verdict: accept the B2 engine mechanism and its nonpublic durable
evidence; broader integration and public content admission remain separate.
