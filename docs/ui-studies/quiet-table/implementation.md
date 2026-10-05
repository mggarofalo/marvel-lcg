# Quiet table implementation and evidence

The selected direction is a stable physical table with contextual action entries,
one editable draft and one commitment. The comparison study is in
[README.md](README.md); the three interactive alternatives are in
[index.html](index.html). The implementation is in the Godot client, with
visibility-safe meaning supplied by Rules, Cards and View.

## From principles to the table

Opposition stays across the table from the player's identity. Hand, installed
assets, piles and local counters keep stable places. Installed upgrades have a
visible face beside the hand, including counters and the named attachment
relation. A revealed encounter card temporarily receives its own space.

Small reading text and comfortable interaction targets are separate choices.
The base body size is 14px, with 12px captions. Contextual commitments and external source controls have a 44px minimum height; card action strips follow the scaled pointer-target metric.
The text-scale setting enlarges the editor and payment chooser; physical cards
retain readable floors and bounded scaling. Dense boards are assessed separately through independent played review.

Action entries belong to their source. Selecting an entry stages the engine's
offer; card inspection remains a separate gesture. Targets and payment sources
edit the same draft. The dock contains the current cause, selected action and
one named commitment. Complete choices temporarily reparents that same editor
into a bounded sheet, preserves the draft and returns focus on dismissal.

Opening replacements and the required phase-end discard choice open directly.
The interface uses Keep hand or the selected discard count. It does not require
discovering a generic hero entry for these required tasks.

The interface removes ornamental eyebrow labels, repeated turn instructions,
free-cost narration and generic confirmation advice. Actual costs, unresolved
effects and validation errors remain visible where the player makes the choice.

## Marvel Champions meaning

Payment distinguishes hand discards from resource abilities that exhaust or
spend counters. Printed damage is distinct from the later authoritative result.
Incoming attacks identify the visible enemy and attacked character, known ATK
and unresolved boost information; defense explains its offered exhaustion and
DEF or redirection without predicting hidden boosts.

Target choices carry a visibility-safe display name separately from their
engine command label. Facedown Drones retain the effective Drone identity in
choices, attack context, retaliation descriptions and completion results.

The latest receipt survives draft changes and includes actual damage, threat,
status, defeat and attack completion. A completed defense can therefore be
reported when no damage was dealt. Attack completion records the established
defender, final attacked character and recorded attack-step damage, including zero.
The engine finalizes early lethal completion after actual damage accounting;
boost damage remains separate from the attack step. Bounded result and decision
regions provide explicit continuation controls. Upright hand actions occupy each
card's exposed fan segment rather than overlapping neighboring source entries.
One physical-card metric owns rendered face size and rotation-safe envelopes;
the hand begins below installed faces with room for its fan and hover lift.
Compact resource icons share the available face width, keeping multi-resource
cards within that physical envelope.

## Contract ownership

Rules and Cards author offer meaning and occurrence facts. View filters those
facts and presents authorized names and results. Godot owns placement, controls,
focus, styling and animation. The renderer does not derive legality from card
text or visible counters.

Protocol version 19 carries additional display and decline labels, the closed attack-completion and applied When Revealed cancellation event variants, the deferred-target boundary, exact commitments, mandatory nonresource costs and passive public pending-decision metadata. Public metadata names the primary answering seat and purpose; causal references include only already readable public, faceup cards. Each seat retains its separately authorized prompt and offers. Its semantic verb is `Attack_Completed`,
distinct from the existing `Attack` effect event. Existing command labels,
seeded randomness and the card-state digest format remain unchanged.

The completion name retains immutable actor facts in the scheduled attack's
existing procedure-owner occurrence. This preserves a Drone's effective name
after retaliation defeats it and reveals the underlying card in its owner's
discard pile. Actual timing occurrences and windows remain separate.

## Review boundary

Independent played review found the initial sheet, source controls, context and
receipt problems. Its correction retest verified substantial fixes, including
source-click staging, actual attack damage, asset counters, incoming context,
resource costs and payment/target resolution. See
[the initial review](native-review-initial.md) and
[the follow-up review](native-review-followup.md) for observed tasks and limits.

The October 4 live review resumed after reboot. It uses the actual Core client
and identifies its reviewed source and assemblies independently. The first
rebuilt batch found six layout and meaning failures, recorded alongside their
corrections and retests in [the resumed report](native-review-final.md) and
[final acceptance](final-acceptance.md). Those records identify the observed
scope, including dense boards and restricted multiplayer roles, and the final
verdict. Headless assertions and synthetic study captures are separate evidence.

## Current correction pass

The fifth candidate was not approved. Its independent report remains the
historical evidence for the required changes, including the ordinary table,
dense Core board and two real restricted clients. The sixth correction pass
assigns one physical face to each displayed instance. Upright labels and source
controls occupy an external sidecar; bounded named region drawers expose
additional sources and preserve the one prompt-bound composer.

Source actions retain their exact copy, current authorized HP and counters,
engine-authored mandatory costs and effect. The engine describes post-villain
minion ordering and anchors it to the engaged-minion area. A restricted client
shows the public answering seat and pending purpose while keeping private
choices private. The sidebar identifies controlled, active and inspected seats,
keeps the latest result visible and explains refresh/retry boundaries. A refresh
that discards a local draft says so and preserves an authoritative ending.

The latest checks and independent verdict are recorded in
[final acceptance](final-acceptance.md),
[correctness review](final-correctness-review.md) and
[the native report](native-review-final.md). A passing source review or automated
rendered checkpoint alone does not establish product readiness. Acceptance requires a new frozen candidate to complete independent played
review of its required scope. The current verdict is recorded in the acceptance
ledger, separate from these implementation mechanisms.

## Historical verification

The September 29 cached-asset runs passed 3,328 selected managed tests, but
excluded 35 socket-dependent cases after sandbox permission failures. Cached
individual wall probes passed; the standard wall scripts and native diagnostics
were not certified in that environment. Those historical results do not certify
the subsequent correction candidates.

On October 4, the rebooted unrestricted workspace supports ordinary socket
validation, official Godot native launches and normal Git operations. The fifth
candidate passed all 3,413 managed tests without skips and the documented
structural and rendered gates, yet its independent native review rejected it.
Every subsequent correction needs its own source/assembly identities and
affected behavioral evidence before merge. Earlier mutations and captures are retained
with their candidate identities; they do not substitute for current validation.

Work is on `feat/marvel-443-contextual-table`, based on
`29bbae99b2b5ced2d5f2cf0ebd2b53a6d3359e22`.
[MARVEL-443](https://plane.wallingford.me/dev/projects/cd677658-41d9-4576-b055-5f2a726b3c85/issues/c687aa25-df1e-44a6-87fe-71d79af5257d)
tracks this increment. Its closure and exact-commit certification are recorded
in the acceptance ledger. The separate compact-viewport increment remains
outside this desktop acceptance boundary.

## Eighth closure corrections

Mandatory Hydra Bomber choices name the resolving identity, amount and current
main-scheme progress. Chosen-player draw choices bind each candidate recipient
before describing draw1. Offers without an installed source control receive a
primary table entry from the same prompt-bound composer.

Rules supplies separate activation-cause IDs from the actual awaited continuation,
including the matching finished activation during its attack-end response. View
filters those IDs and names causes from the authorized snapshot. Ordinary minion
attacks do not claim unresolved boosts unless a boost is actually pending or the
current initiation is eligible to deal one.

Godot renders authorized player-owned reveal areas once, retains attachment
inspection at the host and places named region drawers in bounded source
sidecars. Narrow reveal layouts share the assets slot with its drawer rather
than overlapping identity or allies. Seat roles wrap without character clipping;
card-copy navigation uses Card N of M independently from printed scenario stage.

Latest receipts use the current accepted response, with its offered commitment
and chosen payment sources. Enclosing phase history stays in history. A typed
engine fact reports successful When Revealed cancellation, its source and exact
scope. Played-event cleanup does not displace payment and effect results.
The complete-choice sheet retains current recovery copy through dismissal and
reopening, then clears it when normal progress resumes.

The finite closure record is [final-closure-ledger.md](final-closure-ledger.md).
The eighth independent review completed its ordinary, dense, legal-window and
hosted/recovery journeys but rejected bounded navigation/caption geometry and
an uppercase concealed-back heading. Fixed-width table buttons share compact
caption styling; visible navigation uses short labels with full accessible
purposes, and concealed back headings use quiet display casing. Actual themed
whole-word bounds and the scrolled receipt's reading space are checked.
These corrections require their own source/runtime identities and independent
affected retest; historical evidence cannot approve them. Broader presentation
consolidation and smaller-window work remain separate.

Source selectors, task commitments, the complete-choice entry and invitation controls share the bounded compact treatment. Actual rendered checks reject broken words, a commitment taller than its task viewport and an unused invitation that pushes the supported desktop sidebar outside the window. The separate hosted invitation-layout probe uses a fresh normal restricted game at 100/150 with collapsed/expanded history; the full hosted game fixture uses the documented 1920×1080 desktop boundary and selects actual hand cards for its privacy assertion.


The source chooser owns same-viewport root input through BoardActionChoiceSurface.RouteInput. MainBoardInputRouter retains decision-dialog and pinned-inspector priority, then delegates to that chooser before background board gestures or passive previews. This prevents an unpinned hover preview from consuming the focused chooser's Escape during its dismissal grace; other keys continue through GUI focus handling. The targeted native check distinguishes legal Core gameplay from its synthetic passive-preview visibility case and restores that fixture visibility afterward.
