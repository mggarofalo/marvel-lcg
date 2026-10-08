# Native B1 card inspection

MARVEL-466 shares `CardInspectionContent` between table hover/pinning, pile
browsing, and effective-value source expansion. The full procedural face and
its authorized state remain the same content; each existing surface owns its
placement and input lifetime. This separation was made before changing behavior
and passed 33 focused checks.

Inspection uses the packaged Barlow fonts and canonical resource assets. Full
titles wrap without a two-line limit. Printed rules remain complete, with no
extra printed-rules heading or internal card code. Pile detail measures the full
face and state column, fits beside the transformed source within the viewport,
and updates its frame after native font measurement. Navigation is bounded;
ordinary card details do not claim pile order. Read-only detail does not acquire
a gameplay control or action binding.

Current effective values stay primary. Deliberate source inspection shows the
supplied base, defined/replacement meaning, printed value, named calculation
sources and durations. A source reference resolves to its current authorized
full face when available, preserving cost and current counters. An unavailable
or concealed current face leaves only the supplied public history with no live
target. No renderer arithmetic or identity inference fills gaps.

Card placement uses the transformed four-corner bounds of exhausted and tilted
sources. Hover remains immediate and pointer-transparent, with the existing
source-to-preview grace and drag boundary. Pinned inspection retains explicit
dismissal and source focus. A session reset invalidates the inspector and its
queued focus restoration; a current snapshot still closes pinned detail when
its source is no longer available. Empty action grids are hidden and do not
intercept neighboring controls.

## Automated evidence

The native inspection fixture projects Core Ultron II, Ultron Drones, Ultron's
Imperative, Iron Man, and Mark V Armor. Exhausted poses, a deliberately long
title, and a last-known public source are explicit UI stress snapshots. Every
readable fixture is opened at four viewport corners with actual pointer and
keyboard input at 1920x1080 and 50%, 80%, 100%, and 150%. Assertions cover full
titles/rules, viewport containment, no scrollbar, no concealed entry, no
read-only game action, unchanged source pose, Close/Escape, and restored focus.

A separate seeded Core game opens Ultron through normal setup at 100% and 150%.
It checks the actual top-row hover, complete drone rules, source-to-preview
grace, explicit pinning, Escape, and unchanged revision/prompt. Existing normal
journeys retain overlapping-hand, payment-draft inspection, backdrop dismissal,
keyboard navigation, source replacement during synchronization, and drag checks.
Managed tests cover current/removed/concealed/history snapshots, canonical base
meaning, and rotated placement geometry. Five behavioral mutants were killed:
linking public history to a live target, opening concealed source data, dropping
defined-base meaning, clipping long titles, and restoring the empty action
hitbox. The last mutant initially survived the themed Close-button probe; an
adjacent-control pointer case was added, and it then failed.

The native probe exposed two defects: full titles clipped after two lines, and
an empty action grid covered the Close button above some full faces. Both were
repaired and exercised by the same pointer/keyboard checks. The smoke harness
also learned to retain instance IDs across dismissal, rather than pass a freed
popup as a typed parameter; native diagnostic output remains a gate failure.

## Product review boundary

These are automated headless checks. The earlier Computer Use refusal for Godot
remains binding; no alternate GUI path was attempted. An independent uncoached
review of the visible client and packaged Windows font/rendering verification
remain outstanding. The evidence does not establish product readiness or every
dense-table arrangement.
