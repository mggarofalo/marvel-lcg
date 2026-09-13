# The Table — Astra design study 02

Open [index.html](index.html) at **1920 × 1080**. The bottom review strip selects independent scenes; the top-right control switches between 100% and 150% text/control profiles. This package is a prototype, not a Godot implementation or an engine session.

The proposed organizing principle is **read from the far side of the table toward yourself**. The board expresses who is opposing whom, which objects belong together, and what can be manipulated. The active decision appears beside that relationship. A private hand remains a hand of individually selectable cards at the near edge.

## Source inspection

The visual references were inspected as images, including complete rendered PDF pages. The PDFs and community photos are research inputs held outside the repository; this package does not redistribute them or card scans.

| Source | Observed geometry |
| --- | --- |
| [FFG Learn to Play](https://images-cdn.fantasyflightgames.com/filer_public/ab/be/abbef836-d5ef-4241-b2bd-1062df73f367/mvc01_learn_to_play_eng-compressed.pdf), pp. 4–8 | Identity and health establish a player; deck is nearby; the hand is fanned. Page 8 divides villain/player areas: encounter piles and schemes above the villain, engaged minion above the identity, allies beside it, equipment and supports nearer the player. |
| Same guide, pp. 12, 16 | Basic powers are printed on their acting character. Damage and encounter results are associated with particular cards. A minion, attachment, and side scheme have different spatial destinations. |
| [FFG Rules Reference, official download page](https://www.fantasyflightgames.com/en/products/marvel-champions-the-card-game/), v1.8 pp. 33, 42, 51 | Play areas include out-of-play piles and hands as well as in-play objects. Supports occupy the back row. Setup keeps mulligan discards out of the deck while replacements are drawn. |
| [FFG 1–4 player game mat](https://images-cdn.fantasyflightgames.com/filer_public/f4/81/f481abf9-ddb2-4b2a-9b20-7a052fd13b30/mvs01_gamemat.png) | A wide villain row pairs encounter piles with villain and main scheme, leaving adaptable open space below. |
| [The Solo Meeple real-table photograph](https://thesolomeeple.files.wordpress.com/2020/01/imgp0585.jpg) | A real solo setup provides observational evidence for grouping public opposition at the far edge and the player's cards nearer the viewer. This is community practice, not rules authority. |
| [FFG product presentation](https://www.fantasyflightgames.com/en/products/marvel-champions-the-card-game/) | Card aspect, cost, resource icons, health dials, and small local status components have distinct visual shapes. |

The initial review also inspected a Jesta ThaRogue component photograph. It is a card spread rather than an in-progress table, so it is **not** evidence for play-area placement.

## Independent review outcome

Independent gameplay and implementation reviewers agree that the far-to-near
table is a substantially stronger direction than the earlier dashboard studies.
The confrontation axis, distinct play areas, near-edge hand, contextual draft
markers, and bounded result receipt are worth continuing. They do not consider
this authored prototype a landable specification.

The reviewed direction keeps one player area expanded, as intended, while
retaining compact public summaries for the other players. Those summaries are
not second full tableaux: they expose only the public cooperative context needed
without switching - identity/form/HP, engaged enemies, significant statuses,
and currently offered defenders or other prompt-relevant public objects. The
seat switcher belongs outside the villain area and changes the expanded player
area coherently; it never changes prompt ownership or leaks a concealed hand.

The next iteration must address these findings:

- Only actionable sources advertise an action. Inspection is a separate path;
  selecting an inert card cannot erase the current prompt or focus.
- Affordances, target candidates, and payment generators remain structurally
  distinct parts of one `DecisionComposer`. The fallback cannot flatten them
  into a misleading count of generic choices.
- There is one canonical active control for every offered operation or
  candidate. Table highlights and connectors explain that control; they do not
  introduce a second draft or legality route.
- Keyboard activation currently loses focus after mulligan and target rerenders;
  menus and sheets do not yet receive, trap, or restore focus. Production uses
  stable logical ids, render generations, and current-control reacquisition.
- Center-hit testing missed actual overlap between an ally and upgrade, between
  selected mulligan chrome and the hand/deck, and in narrow inspector chrome.
  Acceptance samples the complete hit rectangle and clipping/occluding siblings.
- Attack arrows, effective scheme thresholds, payment-source anchors, and
  result subjects are not all available as explicit authorized identifiers in
  today's presentation contract. The UI must omit those relationships or add a
  reviewed projection; it must never parse prompt copy or derive rules.
- Connectors need obstacle-aware routing and distinct visual semantics for an
  attack, selected target, payment, attachment, and focus. A line passing through
  a minion cannot imply that the minion intercepted or joined an attack.
- A dense legal fixture must replace fixed sparse slots before the geometry is
  accepted: multiple minions and schemes, three allies, several attachments and
  statuses, unknown/runtime-created areas, and a larger hand all need a bounded
  overflow algorithm.

The reviewers recommend a narrow first implementation slice from `master`:
land lifecycle/error-detection gates, then an engine-backed opening mulligan on
the physical table. The slice auto-enters the single mulligan affordance, uses
the authorized hand candidates as toggles, preserves compact public player
summaries, and commits one composer decision. Ordinary actions, payment,
defense arrows, event animation, and spatial shortcuts follow only after their
presentation contracts and native interaction tests are explicit.

## Geometry grammar

The two play areas and their contents are rules facts. The precise pixel positions, sizes, expansion behavior, and gesture mapping below are presentation choices.

| Relationship | Proposed representation | What the player should infer |
| --- | --- | --- |
| Shared opposition / selected player | Two shallow, differently toned mats with a visible seam | The villain's area belongs to the table; the lower area belongs to the named player. |
| Villain / identity | A stable vertical line of opposing character cards | These are the two sides of the confrontation. |
| Engaged minion / player | Minion just inside the player's side of the seam, above their identity | This enemy is engaged with this player, not a global enemy list. |
| Main scheme / villain | Landscape scheme in the villain row, adjacent to the villain | Threat belongs to a distinct objective, not to a global score. |
| Side schemes | Additional landscape cards beside the main scheme | Each remains its own target with its own threat and icons. This row needs a dense-state fixture before implementation. |
| Identity / allies | Identity anchor with allies on the right flank | Basic powers and defense originate from these characters. |
| Upgrades / supports | A compact band behind the characters; supports sit slightly nearer the hand | Persistent assets remain on the player's mat and can become action/payment sources. |
| Deck / discard | Paired, differently oriented piles on the left of each area | Drawing and discarding are movements between distinct places. |
| Hand / play | Fanned private cards at the near edge | A hand card can be played or spent; its resource face stays visible. |
| Attachment / host | A small connected tab immediately beside its host | An attachment is part of a host relationship, not an unrelated enemy. The lateral tab is the wide-screen adaptation of tucking beneath a card. |
| Health / status / counters | Dials and named markers local to the affected card | State is read where it matters. The current prototype demonstrates health, threat, web counters, and exhaustion; status-card density is unproven. |
| Ready / exhausted | Character face rotates a quarter turn, with a readable upright name/state caption | Exhaustion remains visible without relying on hue. |
| Source / target | A temporary directional connection plus candidate diamonds | The draft is an action between objects. Selection does not itself submit. |

The design uses the horizontal capacity of the screen for piles, schemes, and allied characters, while keeping the villain–minion–identity relationship vertical. It does not reproduce every inch of the printed portrait diagram. The vertical order of upgrades and supports is compressed into a staggered band; that is an explicit compromise to review, not a new game rule.

## Interaction grammar

**Mulligan:** each hand card is a toggle. Selected cards lift and say `DISCARD`; the one commit button changes from `Keep all 6 cards` to `Discard N & draw N`. Inspection is a separate `i` action. Selection never means “keep.”

**Player actions:** select the identity, an ally, or an actionable hand card. A named contextual menu opens beside the confrontation line. The general turn hint temporarily yields to this menu. The selected source keeps its spatial location.

**Target and payment:** the source remains in the hand; legal candidate shapes are marked on the table. Selected target and resource sources receive checkmarks. One composition surface names the draft and its commit. The same hand card is never offered as both the played event and its payment source.

**Defense:** a line from Rhino points toward the attacked character; only offered defender candidates receive diamonds. A chosen defender and `Do not defend` both require the same declaration commit. The unknown boost is named as unknown; no predicted damage total is fabricated.

**Encounter:** the revealed card occupies the villain-side open space before its attachment destination. A presentation acknowledgment places its connected tab at Rhino. A production client would animate only an authorized snapshot/event transition, not ask the UI to decide where it belongs.

**Result:** the sample receipt is beside Rhino, the affected card, and does not consume the next decision's working area.

**All offered choices:** one on-demand sheet contains the entire authored fixture list in stable order. The table is inert while this alternative is open. Choosing there updates the same draft; it does not create a second answer. A production renderer must enumerate the actual engine prompt, including unanchored operations and candidates, exactly once. No operation may disappear because its source type or location is unfamiliar.

**Player switch:** only one player area is expanded. Switching to Captain Marvel shows her own identity, persistent cards, and authorized cooperative hand. It does not transfer the pending Spider-Man decision. `Return to decision` restores that area and its draft. Restricted visibility would have to omit the other hand entirely; the demonstration explicitly uses cooperative visibility.

**Inspector:** at 100%, the tested identity inspector opens in the open space immediately to its left with a connector. At 150%, it opens in a separate readable sheet. The prototype uses a conservative fixed fallback; production placement must measure all obstacles and choose a fallback if no safe nearby rectangle exists.

## Space budget at 1920 × 1080

| Region | Budget | Scale behavior |
| --- | --- | --- |
| Session header | 76 px high | Essential phase and controls grow; secondary session subtitle yields at 150%. |
| Prototype review strip | 46 px high | Review-only chrome; not part of the proposed game client. |
| Actual table | 958 px high | No global scrolling in the represented scene matrix. |
| Villain area | Approximately top 300 px of table | Persistent opposition and pile positions remain stable. |
| Confrontation seam / engaged enemy | Approximately y=300–445 | A compact minion portrait, with local HP, bridges toward the player. |
| Identity and allies | Approximately y=450–655 | Cards retain their anchors. Text grows; decorative art gives up height. |
| Persistent assets | Approximately y=640–755 | Short faces retain names, type, and key current state; full text is inspected. |
| Ordinary hand | Approximately y=755–950 | Five 176 px faces at 100%; five 220 px faces at 150%. |
| Opening hand | Approximately y=675–930 | Six larger 195/229 px faces; empty setup board makes room. |
| Composition | Empty space between player piles and identity | 440 px wide; grows vertically at 150%, preserving table objects. |
| Additional player | Upper-right summary token | Does not introduce a second full mat or a second hand. |

The scale profile enlarges essential text and controls, not the entire canvas. Health numerals are already oversized; ornamental fields contract. The supported design target in this study is 1920×1080 only. Smaller viewport behavior and a dense legal Core Set board are acceptance work, not implied successes.

## Fixtures and honest limits

- Card identities, printed text, types, traits, and attributes are mechanically extracted from `datasets/cards/cards.json` by `extract-cards.cjs`. The UI uses short presentation summaries separately. No scanned card art or network assets are bundled.
- The six review scenes are **independent authored snapshots**, not a deterministic engine trace. The dropdown is deliberately in the review strip.
- The opening fixture uses alter-egos, six cards each, Rhino I at 28 HP, and a 14-threat main-scheme threshold for two players.
- The ordinary snapshot uses Spider-Man Justice, Black Cat, Daredevil, Web-Shooter, Aunt May, a five-card hand, Rhino I at 20/28, and Shocker at 3 HP. The one-of basic resources are not duplicated within that hand. Captain Marvel has a distinct coherent set of Core identity cards when inspected.
- The demonstrated attack is Swinging Web Kick, targeting Rhino, paid by Energy and Web-Shooter. The separate result scene shows Rhino at 12 HP, the event and Energy discarded, and the shooter with two counters/exhausted. This is an authored example, not a legality proof.
- The payment arithmetic, candidate lists, and commit availability are fixture scaffolding. They **must never be copied into Godot or a shared presentation project as rules logic**. Production uses the engine-backed decision composer and prompt revision.
- Mulligan submit and defense declaration end at an explanatory receipt. They do not fabricate a replacement draw or a hidden boost result.
- Basic target selection is sketched. Change form, play a second upgrade, end turn, and pile browsing currently open design notes rather than mutate a game. That is incomplete prototype behavior, not an accepted product interaction.
- The complete fallback is complete for each **authored fixture list**, not claimed complete for every engine prompt. Generic unknown affordances, target allocations, simultaneous costs, interrupted payments, and remote-player defenses still need contract-backed examples.
- Card count totals on the player side are internally balanced in the ordinary and result snapshots. Encounter pile counts are illustrative; no concealed order is modeled.
- A real source-to-target arrow is drawn, but routing around intermediate cards is not solved. Dense attachment, status, minion, and ally arrangements are unproven. An inspector can require fallback even at 100% when a safe slot does not exist.

## Reproduce the visual review

`node docs/table-geometry/render.cjs <capture-directory>` uses Playwright and a local Chromium install. `PLAYWRIGHT_PATH` and `CHROMIUM_PATH` can override the local defaults. It writes captures and the geometry report only to the chosen output directory, never to the repository by default.

The matrix covers six baseline scenes at both profiles, plus selected mulligans, contextual menus, identity inspection, the complete choice sheet, second-player expansion, composed payment, selected defense, and placed attachment. The runner performs actual pointer clicks and reports page errors and center-hit occlusion of visible enabled controls. Deliberately hidden or inert controls are excluded. Passing this test does not prove full text legibility, arbitrary content density, keyboard focus discipline, all control corners, or engine correctness; screenshots and human review remain necessary.

## Acceptance criteria for a landable vertical slice

1. The user can name the villain's area, their area, their engaged enemies, and their private hand from the layout without opening a legend. Seat changes cannot leave the wrong player's name, cards, or choice state in the expanded area.
2. At 100% and 150%, actual pointer and keyboard users can inspect every opening card, select zero/some/all discards, see exactly what will be discarded, and commit once. Repeated input cannot reuse a stale prompt or enqueue focus work on deleted controls.
3. Every engine operation and candidate has one canonical active control, either anchored to the table or rendered in the full ordered fallback. Switching representations preserves the one draft. Unrecognized prompt shapes remain reachable through that fallback.
4. Selection, target allocation, resource generation/declaration, validation, and commit availability come from the authorized composer. The UI must not infer legality or display hidden card identity/order.
5. Identity, allies, minions, attachments, schemes, and hand cards are tested through a real Core engine fixture. The direction of an attack and the relationship of an attachment must remain visually unambiguous.
6. Commit replaces the old decision atomically. Feedback cannot cover the next prompt, pay source, candidate, card title, or essential live value. An in-flight mutation is locked until authoritative resolution or documented recovery.
7. A dense legal Core board at both scales proves area overflow, more than one engaged minion, three allies, multiple persistent assets, multiple schemes, and large hand/candidate sets. Local paging or bounded area scrolling may be used; the viewport itself does not scroll away from the decision.
8. Inspector placement measures the final rendered geometry. If it cannot remain adjacent without obscuring required information, a labeled, focus-contained fallback is used. Escape and Close return focus to the initiating control.
9. Native Godot pointer/focus tests fail on unexpected `ERROR:` output and on controls whose visible rectangles cannot be hit. `is_visible_in_tree()` alone is insufficient evidence.

No production source, existing prototype branch, PR status, or Plane issue is changed by this package. The next implementation decision should follow the Sol geometry/interaction reviews and the user's reaction to the actual table study.
