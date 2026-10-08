# Native B1 candidate cards

MARVEL-467 keeps the existing prompt-bound composer, bounded search gallery,
collection inspector, and engine-owned search completion. `SearchChoiceCard`
separates candidate content and its selection control from gallery paging.
`ClientSceneHost` separates the common table lookup from payment layout before
inspection reuses it. This refactor passed 12 focused checks before behavior
changed.

A candidate body opens the same complete B1 card inspection used by other
collections. Its explicit selection badge alone stages the offered choice.
Read-only inspection cannot play the card, select a target, or commit the
search. It uses the table's authorized art provider and full face/state content.
Pointer and keyboard inspection return to the same candidate; comparison can
advance through the authorized candidates without rebuilding the selection.

Candidate matching follows offered card identities and offer order. Duplicate
titles retain distinct physical identities. Concealed, removed, missing, and
unoffered faces cannot enter the comparison sequence. No card names, printed
rules, or deck counts determine eligibility. The engine supplies the existing
search/look purpose and authorized snapshot; the renderer does not enumerate
candidates before that prompt exists.

The inspection belongs to one live draft generation and its originating face.
Replacing that surface closes only its owned popup and suppresses stale focus
restoration. Paging, return-to-table, reopening, and inspection retain the
single staged choice. The engine's named next commitment and non-cancellable
search boundary remain unchanged.

The gallery retains its content-fitting row of readable full cards and bounded
pages. Full inspection honors the configured scale within the viewport. It does
not introduce a permanent table-wide attachment grid or a card catalog.

## Automated evidence

The normal native gate runs the Black Panther setup search and a legally played
Shuri search at 1920x1080 and 50%, 80%, 100%, and 150%. Black Panther's setup
sequence inspects two candidates with pointer and keyboard both before and after
selection, navigates comparison detail, checks complete rules and no gameplay
actions, and verifies unchanged revision/draft/page and source focus. A resize
replaces the active candidate surface and must close its stale inspector. The
existing paging and return-to-table checks retain the selected logical card.
The temporary narrow viewport is a paging stress probe, not compact-desktop
product certification.

Shuri uses normal Core setup with Black Panther versus Rhino, seed 0. The actual
opening hand contains Shuri and Vibranium. After completing Black Panther's
setup choice, the native journey plays Shuri with Vibranium, observes the search
purpose and Pass without candidate faces, commits the response, inspects the
revealed upgrades, selects through the badge, and completes the engine-owned
search. No post-commit refunding Cancel is offered. Existing authority-cited Core
search tests cover the no-result path and shuffle/information effects; a
no-result engine search does not invent a candidate surface.

Managed matching tests cover duplicate names/order, a single eligible face
among concealed/missing/removed/wrong-namespace references, and zero offered
candidates despite other readable cards. The native Ultron boost journey's
setup selection also uses the explicit badge, preserving its established
engine path. Four behavioral mutants were killed: exposing a concealed
candidate, reversing offered identity order, selecting while inspecting, and
leaving an inspector open after its source surface was replaced.

## Product review boundary

Evidence here is automated headless input and tests. The earlier Computer Use
refusal for Godot remains binding. Independent uncoached review of the visible
client and packaged Windows rendering remain unverified; passing the gates does
not establish product readiness.
