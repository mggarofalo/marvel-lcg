# Decision interface studies

These are interaction and information-architecture studies, not a proposal to
port the current Godot panel arrangement to HTML. Open `index.html` directly in
a desktop browser. No server, network access, images, or build is required.
Use the study toolbar to switch concepts, prompt states, and 100%/150% display.
The target canvas is 1920 × 1080. The 56 px study toolbar is extra overhead that
a production implementation would not need.

## Start with information, not containers

The relevant model is an authorized world snapshot, a seat that owns a prompt,
the prompt's affordances, a draft answer, a validation result, and a request
lifecycle. A playing-card rectangle is optional. A status total is not a legal
action. A result is a past event, not the next task. The viewed player is not
necessarily the answering player. These distinctions drive all three studies.

Printed rules text is useful reference information but need not occupy every
hand item at every moment. Card name, live status, printed cost/resources,
ownership, selection, and offered operations serve different tasks and should
have separate renderings. The same authorized object can be a table object, a
ledger row, or a candidate in an answer, without becoming a different identity.

| Study | Primary interaction | Information architecture | Deliberate cost |
| --- | --- | --- | --- |
| A · Spatial table | Select real visible objects in place; read a transaction receipt before commitment. | Board locations are persistent landmarks. Hand items own mulligan/payment toggles. The right side describes the answer being built. | Familiarity costs area; large object counts need explicit pagination or a list alternative. At 150%, short objects replace tall card forms. |
| B · Decision atlas | Choose from a complete, explicit operation set; compose against a normalized state ledger. | State is a compact table, hand an inventory, decision a complete work area. No card-shaped board is necessary. | Scanning state requires reading; there is less spatial memory or tabletop atmosphere. |
| C · Guided intent | Choose a task, then compose, then confirm, with the current step visible. | A compact state summary persists; choices are grouped by source and task. Full details and the hand inventory remain independently accessible. | Extra navigation can slow expert repetition. Grouping must preserve every affordance and must not invent strategic categories or effects. |

The initial design pass favored B as the baseline and C as the challenger.
Independent gameplay and implementation reviews rejected every study as a
literal implementation baseline. Their converged direction is a task-adaptive,
decision-first workspace: C contributes the shell, A contributes compact
spatial public-state landmarks, and B contributes an exhaustive operation
surface and an optional full-state ledger. C's invented strategy categories and
three-step wizard do not survive review. Grouping may use only explicit
engine-provided source identity, must preserve wire order, and must retain a
complete flat fallback.

The reviewed product model has four distinct layers:

- public state, preserving zones, ownership and object relationships;
- authorized private inventory, independent of the decision owner;
- the current decision, containing every offered operation or candidate once;
- one draft transaction, containing validation, request state, commitment and
  a bounded receipt.

There is one canonical interactive control for each choice. Spatial board or
hand interactions may later replace that control for a supported prompt shape,
but must not duplicate it or create another legality path.

### Independent review findings

The studies exposed useful directions but also reproduced failures that make
them unsuitable as acceptance evidence:

- Bounds-only geometry checks missed real hit occlusion. At 150%, A's result
  covers board controls and B's result covers the final action choices.
- All three seat switchers change labels and concealment without rebuilding a
  coherent seat-specific snapshot. Player statistics, public objects and
  actions can consequently disagree with the displayed identity.
- Illustrative fixtures mix setup, mulligan, player-phase and result state and
  include choices that are not authoritative engine outputs. They cannot
  validate gameplay semantics or legal targeting.
- B/C duplicate candidate controls between the hand and decision surface. One
  unchecked control labeled `Keep` actually adds the card to the replacement
  draft, demonstrating why labels must describe the action they perform.
- B removes relationships and decision-relevant live values needed to reason
  about the board. A assumes a fixed small object grid. C invents a linear
  workflow that does not match every admitted prompt shape.
- At smaller windows and 150% scale, prototype controls can fall below an
  overflow-hidden viewport. A production minimum-size and responsive-overflow
  contract remains an explicit design decision.

The prototypes therefore remain comparative studies. The recommended merge
sequence starts from `master`: first lifecycle and native-input evidence,
second authorized presentation contracts and real fixtures, third a vertical
slice of the adaptive workspace, and only then optional spatial shortcuts and
anchored inspectors.

## Concrete behaviors to review

- Mulligan: all six names remain readable; select replacements in either the
  hand or explicit candidate set, see the count, and choose Replace or Keep
  entire hand. Selection is independent of opening full text.
- Dense ordinary decisions: 12 choices remain onscreen at both scales. C groups
  them by visible source. B exposes one matrix. A preserves the spatial board.
- Composition: selected target, chosen payment sources, change/cancel, validation
  and commitment are separate concepts. Their controls never occupy a card's
  rules-text or live-value rectangle.
- Post-action: the result is compact and refers to History. It does not grow
  with the event list or consume the choice body. History is a separate layer.
- Seats: viewed seat and decision owner remain separately labeled. Switching
  to Captain Marvel demonstrates a concealed hand under a restricted viewer.
- Inspector: Read opens full printed text. At 100%, a hand source can receive
  a source-edge inspector with a visible connector and highlighted origin.
  The 150% study deliberately demonstrates the centered fallback. Escape,
  Close and outside click dismiss; surviving sources receive focus again.

## Space budget

The study shell uses 64 px for global context, 56 px for its comparison toolbar,
32 px for persistent ownership/status, and 16 px outer insets. That leaves an
1888 × 896 px content rectangle, with 16 px between major regions. These are
measured CSS pixels, not render visibility claims.

| Study | 100% allocation | 150% allocation |
| --- | --- | --- |
| A | 1352 px state/hand + 520 px transaction. State 588 px high; hand 292 px, six across. | 1172 px state/hand + 700 px transaction. State 530 px; hand 350 px, three by two. |
| B | 740 px state/inventory + 1132 px decision. State uses a normalized table; inventory is two by three. | 770 px state/inventory + 1102 px decision. Candidates remain two columns; labels wrap. |
| C | Full-width 136 px state summary, 478 px task work area, 250 px inventory. | 144 px summary, 452 px work area, 268 px inventory. Step context moves alongside the question; target and payment sit side by side. |

Base reading type is 18 px at 100% and 27 px at 150%. Secondary semantic roles
have separate sizes; dense object/hand summaries are compact forms rather than
a uniformly magnified tabletop. A production accessibility contract must name
the minimum type for each role and test those actual pixels. The prototype does
not certify every label as exactly 1.5 times its 100% counterpart. In particular,
A trades printed hand metadata for more space at 150%; this is a real tradeoff
for reviewers, not a hidden claim that no information changed.

## State and evidence matrix

`render.cjs` renders 24 screenshots: three concepts × four states × two scales,
plus attached and fallback inspector examples. It checks that headings,
paragraphs, candidates, hand entries, buttons, summaries, and state metrics fit
the viewport and their containing panels. Output is external to the repository:

`C:\Users\mggar\AppData\Local\Temp\marvel-ui-prototypes-20260913`

The initial matrix exposed actual overlap in B's hand and C's composition; the
layout was revised and the final bounds matrix reports no findings. The bounds
check is deliberately weaker than the proposed production reachability checks.
Screenshots were visually inspected for all three 150% layouts and both
inspector placements. There is no claim of a real-game or native Godot pass.

| Coverage | Prototype | Production acceptance |
| --- | --- | --- |
| Mulligan, zero/some/all replacements | Interactive shared selection set | Engine-backed opening deal; real pointer and keyboard confirmation |
| 12 action choices and result present | Render matrix at 100/150 | Longest real labels and every supported scale |
| Target + payment | Illustrative representative composition | Every admitted target/cost/request shape |
| Player switching | Restricted-view example | Both cooperative and restricted snapshots; owner changes while browsing |
| Inspector | Attached and centered fallback | Corners, edges, long text, removed source, resize, focus restoration |
| Repeated confirmation | Local submit latch and revision fence | Real request count and authoritative revision; zero unexpected diagnostics |

## Required acceptance criteria before landing a redesign

1. Obtain every affordance and legal candidate from the prompt/composer. Group
   only by explicit source or engine-provided structure. Preserve a complete
   route for non-card actions, non-visible targets, grouped/ordered/repeated
   selections, variables, resource allocation and simultaneous costs. Do not
   derive legality from card text, health, counters, or the layout.
2. At 1920 × 1080 and every supported scale, all primary context, current
   decision, draft validity and final commitment stay reachable. For each
   clickable control, require its whole hit rectangle inside the viewport and
   every clipping ancestor, and require its center and edge sample points to
   hit that control or its descendants. `IsVisibleInTree` alone cannot pass.
3. Use real pointer/keyboard input through the normal event route. Exercise
   zero/some/all mulligans, 12 long action labels, target selection, change
   action, payment, result presentation, player changes, and long inspectors.
   For bounded collections, verify pages disclose counts and selected items
   and that every candidate is reachable. No item-count guess disables overflow.
4. Submit is synchronously latched before starting a request. Rapid pointer
   clicks, Enter repeats and mixed pointer/keyboard input submit one decision
   for one prompt revision. A sent-but-uncertain request remains locked until
   authoritative synchronization; the UI never repeats the mutation.
5. Deferred focus/scroll work captures a render generation and stable logical
   identity, not a stale Control reference. At execution, verify current
   generation, instance validity, tree membership and actual ancestry before
   using `EnsureControlVisible`. Reacquire the current control or abandon the
   callback. Repeated rerenders produce zero `ERROR:` diagnostics, including
   "Must be an ancestor of the control".
6. Preserve the viewed/answering distinction when prompt ownership changes.
   A seat change rebuilds the displayed workspace and authorized hand from
   the same snapshot. Concealed cards cannot be opened through search,
   inventory, action summaries, inspector links or retained selection.
7. Show a source-edge inspector only when complete placement and a perceptible
   connector fit. Otherwise use an explicit modal fallback. Inspectors contain
   keyboard focus; full text has independent scrolling; dismissal restores
   only a current, valid source. Overlays never accidentally commit a decision.
8. Keep result dimensions bounded independently of event count. The next
   question and commit area cannot move or shrink when history grows. Geometry
   checks and real input checks run on Windows and Linux, and unexpected Godot
   errors fail the smoke rather than being printed beside a success marker.

## Limits of the study

Fixtures are illustrative and intentionally not a legal recorded game. For
example, setup and player-phase state share a board to make space comparisons
controlled; offered choices and target lists are not an engine result. This
must not become new gameplay logic. Printed text for the six inspectable cards
was taken from `datasets/cards/cards.json`; symbols are mockup placeholders,
not a replacement for the client's canonical resource icons.

Action buttons currently enter the same representative target/payment study;
they do not simulate twelve different rules paths. The result example is fixed
text. Pile and identity dialogs are explanatory placeholders. Large collections,
search, grouped and repeated allocations, an error/reconnect flow, real prompt
ownership changes and long-history behavior require production fixtures before
any design receives a merge recommendation. The prototype's browser focus
behavior does not prove Godot's deferred-control lifetime safety.

`render.cjs` currently names the local temporary Playwright installation and
Chromium path used for this review. Those are capture-environment details, not
project dependencies or a proposed CI toolchain.
