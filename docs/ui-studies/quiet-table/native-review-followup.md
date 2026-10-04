# Follow-up independent native review

Reviewed 2026-09-29 against the in-progress working tree built in Debug at
20:24:56 UTC (`/tmp/marvel-design-native-build.log`). The running client was
launched with a requested 1920 × 1080 viewport, solo Spider-Man / Rhino, seed 1.
The reviewer used the native client through CUA, with no click sequence or
Godot implementation inspection. The reviewer authored the engine semantic
tests for this increment; independence applies to the UI implementation and
interaction review, not the semantic contract design.

The native window was zoomed once to verify a suspected empty sheet was not a
stale macOS capture. Screenshots were 1308 × 768 after zoom. Scale 100% and the
live slider's 150% setting were exercised; these observations do not certify a
1920 × 1080 render profile. Automated rendered-profile evidence is separate.

## Tasks and observations

- Mulligan: staging one replacement, opening Complete choices, dismissing with
  Escape, clearing replacements and keeping the hand preserved the draft and
  authoritative state as expected. The opening task remained available after
  clearing. Selected count and the named discard/redraw commitment were clear.
- Card play: Web-Shooter payment distinguished hand discards from Scientist.
  Selecting Scientist enabled Pay and play Web-Shooter. The resulting receipt
  named the played card and generator, and the hand decreased by one.
- Form change: a source entry staged a separate commitment; confirmation
  changed Peter Parker to Spider-Man without exhausting him.
- Basic attack: two hero-to-Rhino drag attempts did not change the draft.
  Repeated clicks on the visible hero action entry opened inspection instead
  of composition. This path could not be completed in this build.
- Phase end: ending the turn reached the required discard task. Selecting its
  hero-local entry exposed staged discard controls. Confirming zero discards
  advanced to the attack interrupt window.
- Spider-Sense: its selected description named the triggering player's draw.
  Committing it increased the hand and decreased the player deck. The next
  defense prompt appeared without a fresh draw receipt.
- Defense: selecting Spider-Man explained exhaustion, DEF 3, remaining damage
  to the hero and unresolved boosts/effects. Defending advanced through the
  attack and encounter reveal to the next turn. The hero was visibly exhausted
  and unharmed; Bomb Scare entered play with three threat.
- At 150%, Swinging Web Kick payment distinguished two hand discards from the
  Web-Shooter resource ability. The generator explanation named exhaustion and
  removal of a web counter. Payment committed before a separate enemy target
  choice. Rhino's offered preview correctly showed 14/14 → 6/14 HP; confirmation
  produced that authoritative state.

## Required fixes

1. Complete choices showed headings and `OPENING HAND · 1 SELECTED · READY`,
   then an empty sheet with neither choices nor commitment. A native redraw
   confirmed the defect. Escape restored the unchanged table draft.
2. Setup, full choices and inspection retained uppercase ornamental labels,
   including the assignment footer and `STATE`, `PRINTED`, `CURRENT`. The user
   explicitly rejected eyebrows. Functional form labels need plain casing.
3. Installed Web-Shooter sat behind the identity with much of its face hidden.
   Its current web counters and resource capacity were not visible. Hand names
   and source labels also truncated heavily, particularly at 150%.
4. The phase-end discard task initially had no hand selectors or commitment,
   requiring discovery of a truncated hero entry. It should be as direct as the
   sole mulligan task. Generic `Choose to continue` does not explain that entry.
5. Incoming attack and defense screens displayed timing boilerplate but omitted
   the visible attacker, attacked character, known ATK and unresolved boosts
   from the main explanation. The current causal context must remain visible.
6. Receipts stayed on prior actions through Spider-Sense and committed event
   payment. Swinging Web Kick's final receipt named play and generators but
   omitted the primary eight-damage result. Nested committed effects and final
   effect results need current authoritative semantic feedback.
7. Swinging Web Kick's target commitment was `Choose option`, although the
   selected target and known effect were available. Name the actual commitment.
8. Payment falsely promised further effect choices for ordinary Web-Shooter
   play, and repeated the turn instruction paragraph. At live scale 150% its
   type appeared unchanged while table type enlarged. Verify the modal's
   scale/profile behavior separately.
9. The hero action entry and drag behavior described above need a native retest
   after correction, including after a previous action has focused the card.

Verdict for this build: changes required. The selected-effect information and
reversible payment/mulligan staging improve comprehension, but the empty full
choice sheet, missing causal context and incomplete results prevent acceptance.
Dense boards, multiplayer, keyboard-only play, grouped/repeated/search/variable
choices and the corrected final build remain unreviewed.

## First correction retest

Retested the restarted Debug client launched into
`/tmp/marvel-design-live-retest100.log`. Same solo Core setup and seed, native
100% followed by the live 150% slider. This build precedes the final setup-pill
and replacement-caption edits. No Godot source was inspected by the reviewer.

Confirmed fixes:

- Complete choices contained all exact opening-hand replacement rows and a
  single Keep hand commitment. Choosing a row and dismissing with Escape
  preserved the same selected card in the table draft. Clearing and keeping
  worked.
- Phase-end discard composition opened directly with hand selectors and Keep
  hand; the prior hidden required-task entry was fixed.
- After a form change focused the hero, the Attack source entry composed the
  offered attack instead of opening inspection. Attack Rhino committed and the
  receipt included Rhino's health changing 14 → 12 plus Spider-Man attacking.
- Web-Shooter was fully visible beside the hand with web-counter value three
  and a named attachment relation. Its prior identity occlusion was fixed.
- Incoming attack/defense explanation now visibly included Rhino, Spider-Man,
  ATK 2 and unresolved facedown boosts. Defense selection retained the DEF 3,
  exhaustion, remaining-damage and later-effects explanation.
- Spider-Sense immediately produced a named draw receipt before defense,
  rather than retaining the old form-change receipt.
- At 150%, payment type enlarged, the scrollable resource list remained usable,
  and progress stated two selected resources with one still needed. Paid event
  costs produced an immediate receipt naming discarded cards, generator
  exhaustion and web-counter change 3 → 2.
- Swinging Web Kick retained its resolution context and engine-authored Rhino
  HP preview. Resolving the selected target changed Rhino 12 → 4 HP; the final
  receipt now included that effect alongside the played card and resources.

Remaining findings in this retest build:

- The deferred enemy entry displayed the raw card code `01094`, and its final
  button said `Choose 01094`. The visible selected-effect description correctly
  named Rhino, so authorized human names were available.
- Zero-damage defense advanced to a receipt about Bomb Scare threat plus
  `Spider-Man resolved End_Phase during the end phase`. It did not name the
  defense result. A keep-hand receipt similarly exposed `Resolve_Mulligans`.
- The installed upgrade included `HOST 1`, an internal identifier. Payment
  retained decorative `Ready to play`/`Not paid yet` captions and a repeated
  player-phase instruction paragraph. Setup retained an empty decorative pill
  and implementation prose in this build.
- Native CUA drag again produced no draft change and left the captured pointer
  at its starting point. Source-click interaction passed; actual native drag
  delivery was not independently established and must not be claimed from it.
- Defense causal text repeated Rhino/ATK in both the context and description.
  Some hand names remained truncated. The exact 1920 profile still needs
  separate rendered evidence.

Verdict for this retest: substantial fixes verified, remaining findings require
correction and retest. Dense boards, multiplayer and complete keyboard-only
play remain outside this review. Native interactions paused before rendered
gates so those captures do not compete with this reviewer for the app.

## Semantic correction evidence

The missing zero-hit defense receipt required an engine fact: `AttackCompleted`
names the enemy, final attacked character and established defender. It does not
claim a damage amount. Occurrence names survive subsequent transitions, and the
View suppresses a completion if any participant is unreadable. The receipt keeps
this fact alongside later phase results.

The final Release solution build passed, followed by 33 focused tests in the
xUnit executable runner: Core decision meaning (9), Core attack completion (2),
completion presentation and privacy (7), legal Core host transport (2), receipt
retention (3), and event vocabulary (10). Eight compiling mutants were killed:
missing emission, permissive participant visibility, omitted useful highlight,
omitted essential highlight, omitted receipt effect, reversed defense wording,
unfiltered occurrence subjects, and missing JSON union registration. All
temporary mutations were restored. This is automated contract evidence; the
native result still requires the final review below.

For the final review, the reviewer additionally authored the attack-completion
contract and the single Godot receipt predicate retaining that event. Layout,
controls, payment and table interactions remain independently reviewed; the
authored receipt predicate is excluded from that independence claim.

## Final build review blocked

The parent reported successful final Release solution and Debug Godot builds
on 2026-09-29, from the uncommitted working tree based on
`29bbae99b2b5ced2d5f2cf0ebd2b53a6d3359e22`. The 33 focused baselines above ran
against that Release build.
The final native review did not start: the GUI launch aborted under the managed
sandbox, and CUA `getApp("org.godotengine.godot")` returned `Computer Use was not
approved to use Godot`. A read-only surface inventory listed Godot as running
but did not provide permission to inspect or operate its window. The reviewer
did not route around that denial.

Verdict: final product acceptance is not established. The first correction
retest remains the latest independent played evidence. Final receipt and target
label fixes, final setup copy, dense boards, multiplayer, native drag delivery
and final 100%/150% behavior remain unverified by this reviewer. Automated
fixtures and screenshots may provide separate regression evidence; they do not
replace the blocked comprehension review. Native Windows/Linux certification
also remains outside this review.

## Post-review semantic regressions

The full Content suite exposed a completion-metadata collision with the
Counter-Punch authority transcript. Completion now uses the engine-chosen verb
`Attack_Completed`, preserving the transcript's count of actual attack effects.

Legal Core regressions also exposed Drone names in three paths: a Backflip
interrupt during a Drone attack, Black Panther's retaliate forecast, and the
completion of an attack whose Drone was defeated by retaliate. These paths now
use the effective public identity. Completion retains immutable actor facts in
an independent owner envelope on the existing EndAttack continuation. Its real
timing occurrence and final target/defender remain separate; the envelope has
no interrupt conditions or spent-window history. An explicitly synthetic
departure-before-damage case tests early continuation replacement without
claiming a printed Core ability that performs that departure.

Attack initiation, defense-ability roles, window execution, attack prompt
context and activation departure were decomposed into cohesive helpers. Attack
and Sequence are both below the 300-line class threshold; their former size
exceptions were removed. No digest fields or canonical save members changed.

After restoration, all 1,710 Content tests and 26 architecture tests passed.
Eight additional compiling mutants were killed: attack-verb collision, missing
actor capture, dropped early-departure preservation, aliased timing history,
missing owner guard, missing actor match, raw prompt actor title, and raw
retaliate actor title. The departure-preservation mutation initially survived
the ordinary retaliate case and prompted the synthetic continuation test that
then killed it. Temporary mutations were restored. These results include Core
Counter-Punch and Spider-Sense transcripts; they do not change the blocked
native acceptance verdict above.
